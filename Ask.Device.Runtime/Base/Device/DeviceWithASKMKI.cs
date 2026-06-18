using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;

namespace Ask.Device.Runtime.Base.Device
{
  /// <summary>
  /// Базовый тип устройства АСК-МКИ, для которого не требуется выбирать транспорт подключения.
  /// </summary>
  public abstract class DeviceWithASKMKI : IDevice
  {
    /// <summary>
    /// Получает или задает идентификатор устройства.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Получает или задает имя устройства.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Получает или задает описание устройства.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Получает или задает номер устройства.
    /// </summary>
    public int Number { get; set; }

    /// <summary>
    /// Получает или задает пустую строку подключения, так как legacy-конфигурация АСК хранится отдельно.
    /// </summary>
    public string ConnectionDetails { get; set; } = string.Empty;

    /// <summary>
    /// Получает или задает тип устройства.
    /// </summary>
    public DeviceType DeviceType { get; set; }

    /// <summary>
    /// Получает или задает полное имя CLR-типа устройства.
    /// </summary>
    public string DeviceClass { get; set; } = string.Empty;

    /// <summary>
    /// Получает или задает менеджер подключения.
    /// </summary>
    public IConnectable ConnectableManager { get; set; } = null!;

    /// <summary>
    /// Получает или задает протокол обмена.
    /// </summary>
    public IDeviceProtocol DeviceProtocol { get; set; } = null!;
  }
}
