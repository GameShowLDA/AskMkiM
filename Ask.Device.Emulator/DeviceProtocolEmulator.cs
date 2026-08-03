using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Chassis;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice;
using Ask.Device.Emulator.Chassis;
using Ask.Device.Emulator.DeviceBusCommutation;
using Ask.Device.Emulator.ModuleRelayControl;
using Ask.Device.Emulator.Multimeter;
using Ask.Device.Emulator.Protocols;
using System.Runtime.CompilerServices;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Device.Emulator
{
  /// <summary>
  /// Создаёт протоколы устройств с поддержкой эмуляции в холостом режиме.
  /// </summary>
  public static class DeviceProtocolEmulator
  {
    private static readonly ConditionalWeakTable<IChassisManager, IDeviceProtocol> Chassis = new();
    private static readonly ConditionalWeakTable<IRelaySwitchModule, IDeviceProtocol> RelaySwitchModules = new();
    private static readonly ConditionalWeakTable<ISwitchingDevice, IDeviceProtocol> SwitchingDevices = new();

    /// <summary>
    /// Выполняет команду мультиметра через реальный протокол или эмулятор и записывает обмен в журнал.
    /// </summary>
    public static async Task<string> QueryMultimeterAsync(
      IMultimeter device,
      string command,
      string idleResponse,
      double responseDelay = 0,
      int timeout = 0,
      int port = 0,
      CancellationToken cancellationToken = default)
    {
      ArgumentNullException.ThrowIfNull(device);

      string mode = ExecutionConfig.GetIsIdleModeEnabled() ? "Холостой режим" : "Реальное обращение";
      string name = $"{device.Name}({device.NumberChassis}.{device.Number})";
      LogInformation($"{mode} | [{name}] Команда мультиметра: \"{command}\".", isDeviceLog: true);

      var protocol = new ModeSelectingDeviceProtocol(
        () => device.DeviceProtocol,
        new MultimeterEmulatorProtocol(idleResponse));
      string response = await protocol.QueryAsync(
        command,
        responseDelay,
        timeout,
        port,
        cancellationToken: cancellationToken);

      LogInformation(
        $"{mode} | [{name}] Ответ мультиметра на \"{command}\": \"{(string.IsNullOrEmpty(response) ? "<пустой>" : response)}\".",
        isDeviceLog: true);
      return response;
    }

    /// <summary>
    /// Создаёт общий протокол эмуляции для экземпляра УКШ.
    /// </summary>
    public static IDeviceProtocol CreateDeviceBusCommutation(ISwitchingDevice device)
    {
      ArgumentNullException.ThrowIfNull(device);
      return SwitchingDevices.GetValue(
        device,
        item => new ModeSelectingDeviceProtocol(
          () => item.DeviceProtocol,
          new DeviceBusCommutationEmulatorProtocol(
            () => item.Number,
            () => item.NumberChassis)));
    }

    /// <summary>
    /// Создаёт общий stateful-протокол эмуляции для экземпляра МКР.
    /// </summary>
    public static IDeviceProtocol CreateModuleRelayControl(IRelaySwitchModule module)
    {
      ArgumentNullException.ThrowIfNull(module);
      return RelaySwitchModules.GetValue(
        module,
        device => CreateModuleRelayControl(
          () => device.DeviceProtocol,
          () => device.Number,
          () => device.NumberChassis));
    }

    /// <summary>
    /// Создаёт протокол модуля коммутации реле с автоматическим выбором режима.
    /// </summary>
    public static IDeviceProtocol CreateModuleRelayControl(
      Func<IDeviceProtocol?> realProtocolProvider,
      Func<int> moduleNumberProvider,
      Func<int> chassisNumberProvider)
    {
      return new ModeSelectingDeviceProtocol(
        realProtocolProvider,
        new ModuleRelayControlEmulatorProtocol(
          moduleNumberProvider,
          chassisNumberProvider));
    }

    /// <summary>
    /// Создаёт протокол контроллера шасси с автоматическим выбором режима.
    /// </summary>
    public static IDeviceProtocol CreateChassis(IChassisManager chassis)
    {
      ArgumentNullException.ThrowIfNull(chassis);
      return Chassis.GetValue(
        chassis,
        device => new ModeSelectingDeviceProtocol(
          () => device.DeviceProtocol,
          new ChassisEmulatorProtocol()));
    }
  }
}
