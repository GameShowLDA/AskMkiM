using static NewCore.Enum.DeviceEnum;

namespace NewCore.Base.Device
{
  /// <summary>
  /// Интерфейс IDevice предоставляет общие методы и свойства для управления устройствами.
  /// </summary>
  public interface IDevice
  {
    /// <summary>
    /// Порядковый номер устройства.
    /// </summary>
    int Id { get; set; }

    /// <summary>
    /// Получает или задает имя устройства.
    /// Имя используется для идентификации устройства в системе.
    /// </summary>
    string Name { get; set; }

    /// <summary>
    /// Получает или задает описание устройства.
    /// Описание содержит дополнительную информацию о функциональности и назначении устройства.
    /// </summary>
    string Description { get; set; }

    /// <summary>
    /// Номер устройства.
    /// </summary>
    int Number { get; set; }

    /// <summary>
    /// Универсальное свойство для хранения данных подключения (IP или COM).
    /// </summary>
    string ConnectionDetails { get; set; }

    /// <summary>
    /// Тип устройства.
    /// </summary>
    public DeviceType DeviceType { get; }

    /// <summary>
    /// Возвращает полный путь к экземпляру класса.
    /// </summary>
    public string DeviceClass { get; set; }

    /// <summary>
    /// Управление состоянием устройства.
    /// </summary>
    public IConnectable ConnectableManager { get; set; }
  }
}