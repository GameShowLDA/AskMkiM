using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static NewCore.Enum.DeviceEnum;

namespace NewCore.Base
{
  /// <summary>
  /// Интерфейс IDevice предоставляет общие методы и свойства для управления устройствами.
  /// </summary>
  public interface IDevice
  {
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
    /// Получает или задает тип соединения устройства.
    /// Тип соединения указывает на способ подключения устройства к системе (например, IP или COM).
    /// </summary>
    ConnectionType ConnectionType { get; set; }

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