using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.DataBase.Provider.Services.Devices;
using Ask.Device.Runtime.Device.ASKMKI;
using Ask.Device.Runtime.Device.Chassi;
using Ask.Engine.Tests.SelfControl.LegacyAskProtocol;
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

    var profile = profileDto.ToProfile();
    bool isIdleMode = ExecutionConfig.GetIsIdleModeEnabled();
    var controller = await CreateControllerAsync(device, profile, isIdleMode, cancellationToken).ConfigureAwait(false);
    var devices = LegacyAskDeviceComposition.Create(profile, controller, device.NumberChassis);

    LogInformation(
      $"Самоконтроль АСК: модуль={module}, стойка={device.NumberChassis}, мультиметр={(meter == null ? "не выбран" : $"{meter.Name}({meter.NumberChassis}.{meter.Number})")}",
      isDeviceLog: true);

    var context = new LegacyAskSelfControlContext(device, profile, userMessageService, cancellationToken, devices, meter);
    await test.ExecuteAsync(context);
  }

  private static async Task<IAskMkiController> CreateControllerAsync(
    LegacyAskSelfControlTarget target,
    Core.Services.Config.LegacyMki.LegacyMkiHardwareProfile profile,
    bool isIdleMode,
    CancellationToken cancellationToken)
  {
    var chassisManagers = await SelfCheckDeviceRuntime.GetChassisManagersAsync(cancellationToken).ConfigureAwait(false);
    var manager = chassisManagers
      .OfType<ManagerASKMKI>()
      .FirstOrDefault(x => x.Number == target.NumberChassis);

    if (manager == null)
    {
      manager = new ManagerASKMKI
      {
        Number = target.NumberChassis,
        Name = target.ChassisName
      };

      LogWarning($"Самоконтроль АСК: стойка {target.NumberChassis} не найдена в БД как ManagerASKMKI, используется runtime-контроллер из legacy-конфига.", isDeviceLog: true);
    }

    manager.IsIdleMode = isIdleMode;
    manager.LegacyProfile = profile;
    manager.UseNetworkProtocol = profile.HardwareAux.Net != 0;
    manager.NetworkAddress = LegacyAskDeviceAddress.Controller;

    LogInformation(
      $"Самоконтроль АСК: контроллер={manager.GetType().Name}, стойка={manager.Number}, idle={isIdleMode}, net={manager.UseNetworkProtocol}, address=0x{manager.NetworkAddress:X2}",
      isDeviceLog: true);

    return manager;
  }
}
