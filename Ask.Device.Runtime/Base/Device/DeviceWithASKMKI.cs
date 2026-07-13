using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;

namespace Ask.Device.Runtime.Base.Device
{
  /// <summary>
  /// Базовый тип устройства АСК-МКИ без внешнего типа подключения.
  /// </summary>
  public abstract class DeviceWithASKMKI : IDevice
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
    /// Получает или задаёт строку параметров подключения.
    /// </summary>
    public string ConnectionDetails { get; set; } = string.Empty;

    /// <summary>
    /// Получает или задаёт тип устройства.
    /// </summary>
    public DeviceType DeviceType { get; set; }

    /// <summary>
    /// Получает или задаёт признак подключаемого устройства.
    /// </summary>
    public bool IsAttachableDevice { get; set; }

    /// <summary>
    /// Получает или задаёт менеджер подключения устройства.
    /// </summary>
    public IConnectable ConnectableManager { get; set; } = null!;

    /// <summary>
    /// Получает или задаёт транспортный протокол устройства.
    /// </summary>
    public IDeviceProtocol DeviceProtocol { get; set; } = null!;
  }
}
