using NewCore.Enum;
using NewCore.Interface;

namespace AppConfig.DataBase.Models
{
  /// <summary>
  /// Класс, представляющий сущность менеджера шасси.
  /// </summary>
  public class ChassisManagerEntity : IChassisManager
  {
    /// <summary>
    /// Название устройства.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Описание устройства.
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Номер устройства в системе.
    /// </summary>
    public int Number { get; set; }

    /// <summary>
    /// Данные соединения (IP-адрес или COM-порт).
    /// </summary>
    public object ConnectionDetails { get; set; }

    /// <summary>
    /// Тип устройства (менеджер шасси).
    /// </summary>
    public DeviceEnum.DeviceType DeviceType => DeviceEnum.DeviceType.ChassisManager;

    /// <summary>
    /// Инициализация устройства.
    /// </summary>
    /// <returns>Возвращает true, если инициализация прошла успешно.</returns>
    public Task<bool> Initialize() => Task.FromResult(true);
  }
}
