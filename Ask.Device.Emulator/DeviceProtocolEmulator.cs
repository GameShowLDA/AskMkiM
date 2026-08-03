using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Chassis;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule;
using Ask.Device.Emulator.Chassis;
using Ask.Device.Emulator.ModuleRelayControl;
using Ask.Device.Emulator.Protocols;
using System.Runtime.CompilerServices;

namespace Ask.Device.Emulator
{
  /// <summary>
  /// Создаёт протоколы устройств с поддержкой эмуляции в холостом режиме.
  /// </summary>
  public static class DeviceProtocolEmulator
  {
    private static readonly ConditionalWeakTable<IChassisManager, IDeviceProtocol> Chassis = new();
    private static readonly ConditionalWeakTable<IRelaySwitchModule, IDeviceProtocol> RelaySwitchModules = new();

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
