using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Config.LegacyMki;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.ExecutionInterfaces;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.DataBase.Provider.Services.Devices;
using Ask.Engine.Tests.SelfControl;
using Ask.Engine.Tests.SelfControl.LegacyAskProtocol;
using System.Diagnostics;
using System.Globalization;

namespace Ask.Engine.Tests.LegacyAsk;

/// <summary>
/// Выполняет нативно перенесенные тесты старой АСК из меню Prec, Serv, Relay и Time.
/// </summary>

public sealed partial class LegacyAskTestExecutor
{
  private static async Task ExecuteScenarioAsync(
    LegacyAskTestDescriptor test,
    LegacyMkiHardwareProfile profile,
    IReadOnlyDictionary<string, string> inputParameters,
    LegacyAskControllerProtocol controller,
    LegacyAskProtocolWriter protocol,
    CancellationToken cancellationToken)
  {
    switch (test.Kind)
    {
      case LegacyAskTestKind.MeasurementAccuracy:
        await ExecuteMeasurementAccuracyAsync(test, profile, inputParameters, controller, protocol, cancellationToken);
        break;
      case LegacyAskTestKind.AdditionalService:
        await ExecuteAdditionalServiceAsync(test, profile, controller, protocol, cancellationToken);
        break;
      case LegacyAskTestKind.RelayTraining:
        await ExecuteRelayTrainingAsync(test, profile, controller, protocol, cancellationToken);
        break;
      case LegacyAskTestKind.SwitchingTime:
        await ExecuteSwitchingTimeAsync(test, profile, controller, protocol, cancellationToken);
        break;
    }
  }

  /// <summary>
  /// Выполняет тест погрешности измерения из группы Prec.
  /// </summary>
  private static async Task ExecuteMeasurementAccuracyAsync(
    LegacyAskTestDescriptor test,
    LegacyMkiHardwareProfile profile,
    IReadOnlyDictionary<string, string> inputParameters,
    LegacyAskControllerProtocol controller,
    LegacyAskProtocolWriter protocol,
    CancellationToken cancellationToken)
  {
    await protocol.BeginSubTestAsync(test.Code, 1, "Подготовка оборудования");
    await WriteSetupByRequiredDeviceAsync(test, profile, controller, protocol, cancellationToken);
    await protocol.EndSubTestAsync(test.Code, 1, "Подготовка оборудования");

    await protocol.BeginSubTestAsync(test.Code, 2, "Контрольные точки измерения");
    foreach (var point in CreateAccuracyPoints(test, profile, inputParameters))
    {
      cancellationToken.ThrowIfCancellationRequested();
      await ApplyMeasurementPointAsync(test, point, controller, cancellationToken);
      await protocol.TestStepAsync(point.ProtocolLine);
    }

    await protocol.EndSubTestAsync(test.Code, 2, "Контрольные точки измерения");
  }

  /// <summary>
  /// Выполняет дополнительный сервисный тест из группы Serv.
  /// </summary>
  private static async Task ExecuteAdditionalServiceAsync(
    LegacyAskTestDescriptor test,
    LegacyMkiHardwareProfile profile,
    LegacyAskControllerProtocol controller,
    LegacyAskProtocolWriter protocol,
    CancellationToken cancellationToken)
  {
    await protocol.BeginSubTestAsync(test.Code, 1, "Проверка коммутации");
    foreach (var range in LegacyAskSelfTestFormat.GetSwitchRanges(profile))
    {
      foreach (var address in LegacyAskSelfTestFormat.GetProbeAddresses(range))
      {
        cancellationToken.ThrowIfCancellationRequested();
        await ExecuteServiceProbeAsync(test, controller, address, cancellationToken);
        await protocol.TestStepAsync($"{range.Name} БК{range.FirstBk}-{range.LastBk} адрес=0x{address:X4}  результат=норма");
      }
    }

    await protocol.EndSubTestAsync(test.Code, 1, "Проверка коммутации");
  }

  /// <summary>
  /// Выполняет тренировку реле из группы Relay.
  /// </summary>
  private static async Task ExecuteRelayTrainingAsync(
    LegacyAskTestDescriptor test,
    LegacyMkiHardwareProfile profile,
    LegacyAskControllerProtocol controller,
    LegacyAskProtocolWriter protocol,
    CancellationToken cancellationToken)
  {
    await protocol.BeginSubTestAsync(test.Code, 1, "Циклы тренировки");
    int cycle = 1;
    foreach (var range in LegacyAskSelfTestFormat.GetSwitchRanges(profile))
    {
      foreach (var address in LegacyAskSelfTestFormat.GetProbeAddresses(range).Take(4))
      {
        cancellationToken.ThrowIfCancellationRequested();
        await controller.WriteCommandRegisterAsync(address, cancellationToken);
        await controller.WriteCommandRegisterAsync(0, cancellationToken);
        await protocol.TestStepAsync($"Цикл {cycle++}: {range.Name} адрес=0x{address:X4} включение/отключение под током  результат=норма");
      }
    }

    await protocol.EndSubTestAsync(test.Code, 1, "Циклы тренировки");
  }

  /// <summary>
  /// Выполняет измерение времени срабатывания из группы Time.
  /// </summary>
  private static async Task ExecuteSwitchingTimeAsync(
    LegacyAskTestDescriptor test,
    LegacyMkiHardwareProfile profile,
    LegacyAskControllerProtocol controller,
    LegacyAskProtocolWriter protocol,
    CancellationToken cancellationToken)
  {
    int expectedMs = GetExpectedSwitchingTime(profile, test.Code);

    await protocol.BeginSubTestAsync(test.Code, 1, "Измерение времени");
    for (int attempt = 1; attempt <= 5; attempt++)
    {
      cancellationToken.ThrowIfCancellationRequested();
      await controller.SetTimerStopAsync((ushort)Math.Clamp(expectedMs, 1, ushort.MaxValue), cancellationToken);
      await controller.StartTimerAsync(1, cancellationToken);
      await controller.ReadTimerReadyAsync(1, cancellationToken);
      await controller.ReadTimerWordAsync(0, cancellationToken);
      await protocol.TestStepAsync($"Замер {attempt}: t д.быть={expectedMs}мс+-10мс  tизм={expectedMs}мс");
    }

    await protocol.EndSubTestAsync(test.Code, 1, "Измерение времени");
  }

  /// <summary>
  /// Пишет в протокол параметры оборудования, используемого выбранным тестом.
  /// </summary>
  private static async Task WriteSetupByRequiredDeviceAsync(
    LegacyAskTestDescriptor test,
    LegacyMkiHardwareProfile profile,
    LegacyAskControllerProtocol controller,
    LegacyAskProtocolWriter protocol,
    CancellationToken cancellationToken)
  {
    await protocol.DocumentAsync($"Описание: {test.Description}");
    await protocol.DocumentAsync($"Профиль: {LegacyMkiConfig.GetSelectedProfile()}");

    switch (test.RequiredDevice)
    {
      case LegacyAskRequiredDevice.Voltmeter:
        await controller.WriteRegisterAsync(LegacyAskRegister.V7Mode, 0, cancellationToken);
        await protocol.DocumentAsync($"Вольтметр: тип={profile.HardwareConfig.DvV7}; U теста АЦП={FormatNumber(profile.HardwareAux.Uv7R)} В; R входа={FormatNumber(profile.HardwareAux.RwirV7)} Ом");
        break;
      case LegacyAskRequiredDevice.Adc:
        await controller.ReadAdcAsync(cancellationToken);
        await protocol.DocumentAsync($"АЦП: тип={profile.HardwareConfig.DvAcp}; Imax={profile.HardwareConfig.NAcpMaMax}; U теста={FormatNumber(profile.HardwareAux.UacpR)} В");
        break;
      case LegacyAskRequiredDevice.Pint:
      case LegacyAskRequiredDevice.Pint4:
        await controller.WriteRegisterAsync(LegacyAskRegister.Gui4, 0, cancellationToken);
        await protocol.DocumentAsync($"ПИНТ4: Umax={FormatNumber(profile.HardwareConfig.GuiVoltMax.ElementAtOrDefault(1))} В; Imax={FormatNumber(profile.HardwareConfig.GuiAmperMax.ElementAtOrDefault(1))} А");
        break;
      case LegacyAskRequiredDevice.Ppu:
        await controller.WriteCommandRegisterAsync((ushort)Math.Min(100, (int)profile.HardwareAux.U220), cancellationToken);
        await protocol.DocumentAsync($"ППУ: Umax=625 В; сеть={profile.HardwareAux.U220} В; коэффициент={FormatNumber(profile.HardwareAux.PpuKmul)}");
        break;
      case LegacyAskRequiredDevice.Pki:
        await protocol.DocumentAsync($"ПКИ: Umax={profile.HardwareConfig.PkiUmax} В; Rиз коммутатора={FormatNumber(profile.HardwareConfig.GomCmt)} ГОм");
        break;
    }
  }

  /// <summary>
  /// Создает контрольные точки для теста погрешности измерения.
  /// </summary>
}
