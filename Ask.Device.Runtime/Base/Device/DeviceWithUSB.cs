using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;

namespace Ask.Device.Runtime.Base.Device
{
  /// <summary>
  /// Представляет базовый тип устройства, подключаемого по USB.
  /// </summary>
  public abstract class DeviceWithUSB : IDevice
  {
    /// <summary>
    /// Получает или задаёт имя устройства.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Получает или задаёт описание устройства.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Получает или задаёт номер устройства.
    /// </summary>
    public int Number { get; set; }

    /// <summary>
    /// Получает или задаёт идентификатор устройства.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Получает или задаёт полное имя CLR-типа устройства.
    /// </summary>
    public string DeviceClass { get; set; } = string.Empty;

    /// <summary>
    /// Получает или задаёт тип устройства.
    /// </summary>
    public DeviceType DeviceType { get; set; }

    /// <summary>
    /// Получает или задаёт строку подключения USB-устройства.
    /// Используется как шаблон имени для поиска в системе.
    /// </summary>
    public string ConnectionDetails
    {
      get => _connectionDetails;
      set
      {
        _connectionDetails = value;
      }
    }

    /// <summary>
    /// Получает или задаёт менеджер подключения устройства.
    /// </summary>
    public IConnectable ConnectableManager { get; set; } = null!;

    /// <summary>
    /// Получает или задаёт транспортный протокол устройства.
    /// </summary>
    public IDeviceProtocol DeviceProtocol { get; set; } = null!;

    /// <summary>
    /// Хранит строку шаблона для поиска USB-устройства.
    /// </summary>
    private string _connectionDetails = string.Empty;
  }
}
