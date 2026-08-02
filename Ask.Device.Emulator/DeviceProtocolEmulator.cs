using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Chassis;
using Ask.Device.Emulator.Chassis;
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