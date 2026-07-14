using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.DataBase.Provider.Services.Devices;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Engine.Tests.SelfControl;

/// <summary>
/// Запускает самоконтроль старого тестера АСК по выбранному устройству.
/// </summary>
public sealed class SelfTestManager
{
  private readonly IReadOnlyDictionary<LegacyAskSelfControlModule, LegacyAskModuleTestBase> _tests;

  /// <summary>
  /// Создаёт менеджер самоконтроля старой АСК.
  /// </summary>
  public SelfTestManager()
  {
    LegacyAskModuleTestBase[] tests =
    [
      new LegacyAskDigitalVoltmeterSelfControlTest(),
      new LegacyAskAdcSelfControlTest(),
      new LegacyAskDeviceSwitchingSelfControlTest(),
      new LegacyAskPintsSelfControlTest(),
      new LegacyAskCommutatorSelfControlTest(),
      new LegacyAskPpuSelfControlTest(),
      new LegacyAskPkiSelfControlTest(),
      new LegacyAskTimerSelfControlTest()
    ];

    _tests = tests.ToDictionary(test => test.Module);
  }

  /// <summary>
  /// Возвращает тип перечисления, используемый для модулей самоконтроля старой АСК.
  /// </summary>
  public Type GetTestTypeEnum()
  {
    return typeof(LegacyAskSelfControlModule);
  }

  /// <summary>
  /// Запускает самоконтроль выбранного модуля старой АСК.
  /// </summary>
  public async Task StartSelfCheck(
    CancellationToken cancellationToken,
    Enum? selectedType,
    IUserInteractionService? userMessageService = null,
    LegacyAskSelfControlTarget? device = null,
    IMultimeter? meter = null)
  {
    if (userMessageService == null)
    {
      return;
    }

    if (device == null)
    {
      await userMessageService.ShowMessageAsync(new ShowMessageModel("Ошибка самоконтроля АСК", message: "Не выбрана стойка старой АСК.", type: ShowMessageModel.MessageType.Error));
      return;
    }

    var module = device.Module;
    if (!_tests.TryGetValue(module, out var test))
    {
      await userMessageService.ShowMessageAsync(new ShowMessageModel("Ошибка самоконтроля АСК", message: "Не найден обработчик выбранного модуля.", type: ShowMessageModel.MessageType.Error));
      return;
    }

    var service = new LegacyMkiHardwareProfileDtoService();
    await service.EnsureDefaultProfilesAsync(device.NumberChassis, cancellationToken);

    var profileKind = LegacyMkiConfig.GetSelectedProfile();
    var profileDto = await service.GetByChassisAsync(device.NumberChassis, profileKind, cancellationToken);
    if (profileDto == null)
    {
      await userMessageService.ShowMessageAsync(new ShowMessageModel("Ошибка самоконтроля АСК", message: $"Не найдена конфигурация стойки {device.NumberChassis}.", type: ShowMessageModel.MessageType.Error));
      return;
    }

    LogInformation(
      $"Самоконтроль АСК: модуль={module}, стойка={device.NumberChassis}, мультиметр={(meter == null ? "не выбран" : $"{meter.Name}({meter.NumberChassis}.{meter.Number})")}",
      isDeviceLog: true);

    var context = new LegacyAskSelfControlContext(device, profileDto.ToProfile(), userMessageService, cancellationToken, meter);
    await test.ExecuteAsync(context);
  }
}
