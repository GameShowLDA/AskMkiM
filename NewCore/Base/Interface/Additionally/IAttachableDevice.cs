using NewCore.Base.Device;

namespace NewCore.Base.Interface.Additionally
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
  }
}
