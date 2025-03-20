using NewCore.Base.Device;
using NewCore.Base.Interface.Main;

namespace NewCore.Device
{
  /// <summary>
  /// Класс <see cref="Test"/> представляет устройство стойки СКМ с подключением по IP-адресу.
  /// </summary>
  public class Test : DeviceWithIP, IRack
  {
    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="Test"/>.
    /// </summary>
    public Test() { }

    /// <summary>
    /// Получает название устройства.
    /// </summary>
    public string Name => "Стойка СКМ";

    /// <summary>
    /// Получает описание устройства.
    /// </summary>
    public string Description => "Добавить описание сюда";

    /// <summary>
    /// Получает или задает номер шасси, к которому подключено устройство.
    /// </summary>
    public int NumberChassis { get; set; }
  }
}
