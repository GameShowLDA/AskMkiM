using Ask.Core.Shared.Interfaces.DeviceInterfaces.Capabilities;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums;

namespace Ask.Core.Shared.Interfaces.DeviceInterfaces.Chassis
{
  /// <summary>
  /// Интерфейс для менеджера шасси.
  /// </summary>
  public interface IChassisManager : IDevice, IHeadUnit
  {
    /// <summary>
    /// Управление питанием шасси.
    /// </summary>
    IPower PowerManager { get; set; }
  }
}
