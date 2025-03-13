using static NewCore.Enum.DeviceEnum;

namespace NewCore.Base
{
  /// <summary>
  /// Интерфейс IDevice предоставляет общие методы и свойства для управления устройствами.
  /// </summary>
  public interface IDevice
  {
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
    /// Номер утсройства.
    /// </summary>
    int Number { get; set; }

    /// <summary>
    /// Универсальное свойство для хранения данных подключения (IP или COM)
    /// </summary>
    string ConnectionDetails { get; set; }

    /// <summary>
    /// Тип устройства.
    /// </summary>
    DeviceType DeviceType { get; }

    /// <summary>
    /// Проверяет соединение с устройством.
    /// Метод выполняет проверку наличия соединения с устройством и возвращает результат.
    /// </summary>
    /// <returns>
    /// Возвращает <see cref="bool"/>, указывающий на наличие соединения:
    /// <c>true</c> — если соединение установлено, <c>false</c> — в противном случае.
    /// </returns>
    Task<bool> Initialize();
  }
}