using System.ComponentModel.DataAnnotations.Schema;
using NewCore.Base.Function.ModuleVoltageCurrentSource;
using NewCore.Base.Interface.Main;
using NewCore.Enum;

namespace AppConfig.DataBase.Models
{
  /// <summary>
  /// Класс, представляющий сущность модуля источника питания.
  /// </summary>
  public class PowerSourceModuleEntity : IPowerSourceModule
  {
    public int Id { get; set; }
    /// <summary>
    /// Номер менеджера шасси, к которому подключен модуль.
    /// </summary>
    public int NumberChassis { get; set; }

    /// <summary>
    /// Название модуля источника питания.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Описание модуля источника питания.
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Уникальный номер устройства.
    /// </summary>
    public int Number { get; set; }

    /// <summary>
    /// Детали подключения модуля (IP-адрес, COM-порт и т. д.).
    /// </summary>
    public string ConnectionDetails { get; set; }

    /// <summary>
    /// Тип устройства, всегда PowerSourceModule.
    /// </summary>
    public DeviceEnum.DeviceType DeviceType => DeviceEnum.DeviceType.PowerSourceModule;

    public string DeviceClass { get; set; }

    [NotMapped]
    public IBusManager BusManager { get; set; }
    [NotMapped]
    public ICurrentManager CurrentManager { get; set; }
    [NotMapped]
    public IStateManager StateManager { get; set; }
    [NotMapped]
    public IVoltageManager VoltageManager { get; set; }


    /// <summary>
    /// Метод инициализации модуля источника питания.
    /// </summary>
    /// <returns>Возвращает true, если инициализация прошла успешно.</returns>
    public Task<bool> Initialize() => Task.FromResult(true);
  }
}
