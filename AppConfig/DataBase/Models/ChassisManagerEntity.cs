using System.ComponentModel.DataAnnotations.Schema;
using NewCore.Base.Function.ManagerChassis;
using NewCore.Base.Interface.Additionally;
using NewCore.Base.Interface.Main;
using NewCore.Enum;

namespace AppConfig.DataBase.Models
{
  /// <summary>
  /// Класс, представляющий сущность менеджера шасси.
  /// </summary>
  public class ChassisManagerEntity : IChassisManager, IHeadUnit
  {
    public int Id { get; set; }
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
    public string ConnectionDetails { get; set; }

    /// <summary>
    /// Тип устройства (тестер АСКМ).
    /// </summary>
    public DeviceEnum.DeviceType DeviceType => DeviceEnum.DeviceType.ChassisManager;

    public string DeviceClass { get; set; }

    [NotMapped]
    public IStateManagerChassis StateManager { get; set; }

    [NotMapped]
    public IPowerManagerChassis PowerManager { get; set; }

    /// <summary>
    /// Инициализация устройства.
    /// </summary>
    /// <returns>Возвращает true, если инициализация прошла успешно.</returns>
    public Task<bool> Initialize() => Task.FromResult(true);
  }
}
