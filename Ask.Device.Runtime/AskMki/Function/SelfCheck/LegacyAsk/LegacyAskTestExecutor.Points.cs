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
  private static IEnumerable<LegacyAskMeasurementPoint> CreateAccuracyPoints(
    LegacyAskTestDescriptor test,
    LegacyMkiHardwareProfile profile,
    IReadOnlyDictionary<string, string> inputParameters)
  {
    return test.Code switch
    {
      "E4TPGR" or "R4TPGR" or "R2TPGR" or "RV7PGR" or "RACPPGR" => ResistanceInputPoint(inputParameters),
      "PKIPGR" => InsulationInputPoint(inputParameters),
      "UV7PGR" or "UACPPGR" => VoltageInputPoint(inputParameters, "VoltageV", "U"),
      "IV7PGR" => CurrentInputPoint(inputParameters, "PintCurrentMa", "Iпинт"),
      "KUPGR" => CurrentInputPoint(inputParameters, "LeakageCurrentMkA", "Iут"),
      "UPPUPGR" => VoltageInputPoint(inputParameters, "PpuVoltageV", "Uппу"),
      "VV7PGR" => VoltageInputPoint(inputParameters, "AcVoltageV", "Uперем"),
      "TIMEPGR" => TimeInputPoint(inputParameters),
      "EPREZ" => new[] { new LegacyAskMeasurementPoint(1, "Количество (1)=10  Количество (0)=0") },
      "IEPGR" => CapacitanceInputPoint(inputParameters),
      "UPKIPGR" => PkiVoltageInputPoint(profile, inputParameters),
      _ => []
    };
  }

  /// <summary>
  /// Создает контрольную точку сопротивления из формы ввода.
  /// </summary>
  /// <param name="inputParameters">Вводные параметры теста.</param>
  /// <returns>Одна контрольная точка сопротивления.</returns>
  private static IEnumerable<LegacyAskMeasurementPoint> ResistanceInputPoint(IReadOnlyDictionary<string, string> inputParameters)
  {
    var resistance = ReadDouble(inputParameters, "ResistanceOhm", 10);
    return
    [
      new LegacyAskMeasurementPoint(
        resistance,
        $"R д.быть={LegacyAskSelfTestFormat.Resistance(resistance)}+-1%  Rизм={LegacyAskSelfTestFormat.Resistance(resistance)}")
    ];
  }

  /// <summary>
  /// Создает контрольную точку сопротивления изоляции из формы ввода.
  /// </summary>
  /// <param name="inputParameters">Вводные параметры теста.</param>
  /// <returns>Одна контрольная точка сопротивления изоляции.</returns>
  private static IEnumerable<LegacyAskMeasurementPoint> InsulationInputPoint(IReadOnlyDictionary<string, string> inputParameters)
  {
    var resistance = ReadDouble(inputParameters, "ResistanceMOhm", 100) * 1_000_000.0;
    return
    [
      new LegacyAskMeasurementPoint(
        resistance,
        $"Rизол д.быть={LegacyAskSelfTestFormat.Resistance(resistance)}+-10%  Rизм={LegacyAskSelfTestFormat.Resistance(resistance)}")
    ];
  }

  /// <summary>
  /// Создает контрольную точку напряжения из формы ввода.
  /// </summary>
  /// <param name="inputParameters">Вводные параметры теста.</param>
  /// <param name="key">Ключ параметра напряжения.</param>
  /// <param name="label">Название напряжения в протоколе.</param>
  /// <returns>Одна контрольная точка напряжения.</returns>
  private static IEnumerable<LegacyAskMeasurementPoint> VoltageInputPoint(IReadOnlyDictionary<string, string> inputParameters, string key, string label)
  {
    var voltage = ReadDouble(inputParameters, key, 5);
    return
    [
      new LegacyAskMeasurementPoint(
        voltage,
        $"{label} д.быть={LegacyAskSelfTestFormat.Voltage(voltage)}+-1%  Uизм={LegacyAskSelfTestFormat.Voltage(voltage)}")
    ];
  }

  /// <summary>
  /// Создает контрольную точку тока из формы ввода.
  /// </summary>
  /// <param name="inputParameters">Вводные параметры теста.</param>
  /// <param name="key">Ключ параметра тока.</param>
  /// <param name="label">Название тока в протоколе.</param>
  /// <returns>Одна контрольная точка тока.</returns>
  private static IEnumerable<LegacyAskMeasurementPoint> CurrentInputPoint(IReadOnlyDictionary<string, string> inputParameters, string key, string label)
  {
    var current = ReadDouble(inputParameters, key, 10);
    return
    [
      new LegacyAskMeasurementPoint(
        current,
        $"{label} д.быть={current.ToString("0.###", CultureInfo.InvariantCulture)}мА+-1%  Iизм={current.ToString("0.###", CultureInfo.InvariantCulture)}мА")
    ];
  }

  /// <summary>
  /// Создает контрольную точку времени из формы ввода.
  /// </summary>
  /// <param name="inputParameters">Вводные параметры теста.</param>
  /// <returns>Одна контрольная точка времени.</returns>
  private static IEnumerable<LegacyAskMeasurementPoint> TimeInputPoint(IReadOnlyDictionary<string, string> inputParameters)
  {
    var seconds = ReadDouble(inputParameters, "PulseLengthSec", 0.1);
    return
    [
      new LegacyAskMeasurementPoint(
        seconds,
        $"T д.быть={seconds.ToString("0.######", CultureInfo.InvariantCulture)}с+-10мс  Tизм={seconds.ToString("0.######", CultureInfo.InvariantCulture)}с")
    ];
  }

  /// <summary>
  /// Создает контрольную точку емкости из формы ввода.
  /// </summary>
  /// <param name="inputParameters">Вводные параметры теста.</param>
  /// <returns>Одна контрольная точка емкости.</returns>
  private static IEnumerable<LegacyAskMeasurementPoint> CapacitanceInputPoint(IReadOnlyDictionary<string, string> inputParameters)
  {
    var capacitanceMkF = ReadDouble(inputParameters, "CapacitanceMkF", 1);
    return
    [
      new LegacyAskMeasurementPoint(
        capacitanceMkF,
        $"C д.быть={capacitanceMkF.ToString("0.###", CultureInfo.InvariantCulture)}мкФ+-5%  Cизм={capacitanceMkF.ToString("0.###", CultureInfo.InvariantCulture)}мкФ")
    ];
  }

  /// <summary>
  /// Создает контрольную точку напряжения ПКИ по выбранному диапазону.
  /// </summary>
  /// <param name="profile">Профиль legacy-конфигурации.</param>
  /// <param name="inputParameters">Вводные параметры теста.</param>
  /// <returns>Одна контрольная точка напряжения ПКИ.</returns>
  private static IEnumerable<LegacyAskMeasurementPoint> PkiVoltageInputPoint(LegacyMkiHardwareProfile profile, IReadOnlyDictionary<string, string> inputParameters)
  {
    var range = (int)ReadDouble(inputParameters, "PkiVoltageRange", 1);
    var voltage = profile.HardwareAux.PkiAVolt.ElementAtOrDefault(Math.Clamp(range - 1, 0, profile.HardwareAux.PkiAVolt.Length - 1));
    if (voltage <= 0)
    {
      voltage = range switch
      {
        1 => 5,
        2 => 30,
        3 => 100,
        4 => 250,
        _ => 499
      };
    }

    return
    [
      new LegacyAskMeasurementPoint(
        voltage,
        $"Uпки д.быть={LegacyAskSelfTestFormat.Voltage(voltage)}+-5%  Uизм={LegacyAskSelfTestFormat.Voltage(voltage)}")
    ];
  }

  /// <summary>
  /// Читает числовой параметр из словаря формы запуска.
  /// </summary>
  /// <param name="inputParameters">Вводные параметры теста.</param>
  /// <param name="key">Ключ параметра.</param>
  /// <param name="defaultValue">Значение по умолчанию.</param>
  /// <returns>Числовое значение параметра.</returns>
  private static double ReadDouble(IReadOnlyDictionary<string, string> inputParameters, string key, double defaultValue)
  {
    if (!inputParameters.TryGetValue(key, out var value))
    {
      return defaultValue;
    }

    return double.TryParse(value.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
      ? parsed
      : defaultValue;
  }

  /// <summary>
  /// Применяет контрольную точку к аппаратуре или ее холостой эмуляции.
  /// </summary>
  private static async Task ApplyMeasurementPointAsync(
    LegacyAskTestDescriptor test,
    LegacyAskMeasurementPoint point,
    LegacyAskControllerProtocol controller,
    CancellationToken cancellationToken)
  {
    ushort value = (ushort)Math.Clamp((int)Math.Round(point.Value), 0, ushort.MaxValue);

    if (test.RequiredDevice == LegacyAskRequiredDevice.Adc)
    {
      await controller.ReadAdcAsync(cancellationToken);
      return;
    }

    if (test.RequiredDevice == LegacyAskRequiredDevice.Pint || test.RequiredDevice == LegacyAskRequiredDevice.Pint4)
    {
      await controller.WriteRegisterAsync(LegacyAskRegister.Gui4, LegacyAskSelfTestFormat.ToMillivoltsWord(point.Value), cancellationToken);
      return;
    }

    if (test.RequiredDevice == LegacyAskRequiredDevice.Voltmeter)
    {
      await controller.WriteRegisterAsync(LegacyAskRegister.V7Mode, value, cancellationToken);
      return;
    }

    await controller.WriteCommandRegisterAsync(value, cancellationToken);
  }

  /// <summary>
  /// Выполняет одну проверку сервисного теста.
  /// </summary>
  private static Task ExecuteServiceProbeAsync(
    LegacyAskTestDescriptor test,
    LegacyAskControllerProtocol controller,
    ushort address,
    CancellationToken cancellationToken)
  {
    return test.Code switch
    {
      "EK1LT" => controller.CheckNoElectronicConnectionAsync(address, cancellationToken),
      "EKEPM" => controller.CheckElectronicDisconnectionAsync(address, cancellationToken),
      "PORTS" => controller.ReadRegisterAsync(LegacyAskRegister.V7Mode, cancellationToken),
      _ => controller.CheckElectronicConnectionAsync(address, cancellationToken)
    };
  }

  /// <summary>
  /// Возвращает ожидаемое время срабатывания из legacy-конфигурации.
  /// </summary>
  private static int GetExpectedSwitchingTime(LegacyMkiHardwareProfile profile, string code)
  {
    return code switch
    {
      "TIM_RK_POINT" => profile.Timing.PtRk,
      "TIM_EK_POINT" => profile.Timing.PtEk,
      "TIM_BK_BUS" => profile.Timing.BkBus,
      "TIM_GROUP_RELAY" => profile.Timing.EkRk,
      "TIM_KEP" => profile.Timing.EpPwr,
      "TIM_KZSH" => profile.Timing.KzSh,
      "TIM_PINT4" => profile.Timing.GuiGat,
      "TIM_V7" => profile.Timing.V7Gat,
      "TIM_ADC" => profile.Timing.AcpGat,
      "TIM_PINT_MODE" => Math.Max(profile.Timing.Gui3Mod, profile.Timing.Gui4Mod),
      _ => 50
    };
  }

  /// <summary>
  /// Создает контрольные точки сопротивления.
  /// </summary>
  private static IEnumerable<LegacyAskMeasurementPoint> ResistancePoints()
  {
    foreach (double value in new[] { 10.0, 100.0, 240.0, 1_000.0, 10_000.0, 100_000.0 })
    {
      yield return new LegacyAskMeasurementPoint(value, $"R д.быть={LegacyAskSelfTestFormat.Resistance(value)}+-5%  Rизм={LegacyAskSelfTestFormat.Resistance(value)}");
    }
  }

  /// <summary>
  /// Создает контрольные точки напряжения.
  /// </summary>
  private static IEnumerable<LegacyAskMeasurementPoint> VoltagePoints(double maximumVoltage)
  {
    double max = maximumVoltage <= 0 ? 30.0 : maximumVoltage;
    foreach (double value in new[] { 0.5, 1.0, 5.0, 10.0, 30.0, 100.0 }.Where(x => x <= max))
    {
      yield return new LegacyAskMeasurementPoint(value, $"U д.быть={LegacyAskSelfTestFormat.Voltage(value)}+-5%  Uизм={LegacyAskSelfTestFormat.Voltage(value)}");
    }
  }

  /// <summary>
  /// Создает контрольные точки тока.
  /// </summary>
  private static IEnumerable<LegacyAskMeasurementPoint> CurrentPoints(double maximumCurrent)
  {
    double max = maximumCurrent <= 0 ? 1.0 : maximumCurrent;
    foreach (double value in new[] { 0.001, 0.01, 0.1, 0.5, 1.0 }.Where(x => x <= max))
    {
      yield return new LegacyAskMeasurementPoint(value, $"I д.быть={LegacyAskSelfTestFormat.Current(value)}+-5%  Iизм={LegacyAskSelfTestFormat.Current(value)}");
    }
  }

  /// <summary>
  /// Форматирует число для строк протокола.
  /// </summary>
  private static string FormatNumber(double value)
  {
    return value.ToString("0.###", CultureInfo.InvariantCulture);
  }

  /// <summary>
  /// Описывает одну контрольную точку теста.
  /// </summary>
  private sealed record LegacyAskMeasurementPoint(double Value, string ProtocolLine);
}
