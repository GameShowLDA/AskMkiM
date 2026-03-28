using Ask.Core.Shared.DTO.Devices.Breakdown;
using Ask.Core.Shared.DTO.Devices.ChassisManager;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Capabilities;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums;

namespace Ask.Core.Shared.Interfaces.DeviceInterfaces.Chassis
{
  /// <summary>
  /// Интерфейс для менеджера шасси.
  /// </summary>
  public interface IChassisManager : IHeadUnit, IDeviceToDtoConverter<ChassisManagerDto>
  {
    /// <summary>
    /// Управление питанием шасси.
    /// </summary>
    IPower PowerManager { get; set; }
  }
}
