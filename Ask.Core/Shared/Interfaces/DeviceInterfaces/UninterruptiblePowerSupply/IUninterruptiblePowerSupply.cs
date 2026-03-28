using Ask.Core.Shared.DTO.Devices.ChassisManager;
using Ask.Core.Shared.DTO.Devices.UninterruptiblePowerSupply;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Capabilities;

namespace Ask.Core.Shared.Interfaces.DeviceInterfaces.UninterruptiblePowerSupply
{
  /// <summary>
  /// Общий интерфейс бесперебойников.
  /// </summary>
  public interface IUninterruptiblePowerSupply : IAttachableDevice, IDeviceToDtoConverter<UninterruptiblePowerSupplyDto>
  {
    /// <summary>
    /// Получает или задаёт реализацию управления питанием UPS.
    /// </summary>
    public IPower PowerManager { get; set; }

    /// <summary>
    /// Последний найденный системный путь устройства.
    /// </summary>
    string LastResolvedDevicePath { get; set; }
  }
}
