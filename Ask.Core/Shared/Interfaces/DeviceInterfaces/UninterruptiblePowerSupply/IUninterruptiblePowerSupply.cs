using Ask.Core.Shared.Interfaces.DeviceInterfaces.Capabilities;

namespace Ask.Core.Shared.Interfaces.DeviceInterfaces.UninterruptiblePowerSupply
{
  /// <summary>
  /// Общий интерфейс бесперебойников.
  /// </summary>
  public interface IUninterruptiblePowerSupply : IAttachableDevice, IPower
  {
    /// <summary>
    /// Последний найденный системный путь устройства.
    /// </summary>
    string LastResolvedDevicePath { get; set; }
  }
}
