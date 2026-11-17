namespace Ask.Core.Shared.Interfaces.DeviceInterfaces
{
  /// <summary>
  /// Интерфейс устройства, которое можно подключать к шасси.
  /// Наследует основные свойства и методы устройства из <see cref="IDevice"/>.
  /// </summary>
  public interface IAttachableDevice : IDevice
  {
    /// <summary>
    /// Получает или задаёт номер менеджера шасси, к которому подключено устройство.
    /// </summary>
    int NumberChassis { get; set; }

    /// <summary>
    /// Получает или задаёт пары структурных шин, доступные устройству.
    /// </summary>
    Ask.Core.Shared.Metadata.Enums.DeviceEnums.SupportedBusPairs SupportedBusPairs { get; set; }
  }
}
