using Ask.Core.Shared.Interfaces.DeviceInterfaces.Chassis.Capabilities;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums;

namespace Ask.Core.Shared.Interfaces.DeviceInterfaces.Chassis
{
  /// <summary>
  /// Интерфейс для менеджера шасси.
  /// </summary>
  public interface IChassisManager : IDevice, IHeadUnit
  {
    BusStructureEnum.Type BusType { get; set; }

    /// <summary>
    /// Управление питанием шасси.
    /// </summary>
    IPowerManagerChassis PowerManager { get; set; }
  }
}
