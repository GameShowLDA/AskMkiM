using NewCore.Enum;
using NewCore.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppConfig.DataBase.Models
{
  /// <summary>
  /// Класс, представляющий сущность модуля коммутации реле.
  /// </summary>
  public class RelaySwitchModuleEntity : IRelaySwitchModule
  {
    /// <summary>
    /// Номер менеджера шасси, к которому подключен модуль.
    /// </summary>
    public int NumberChassis { get; set; }

    /// <summary>
    /// Количество точек коммутации в модуле.
    /// </summary>
    public int PointCount { get; set; }

    /// <summary>
    /// Название модуля.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Описание модуля.
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Уникальный номер устройства.
    /// </summary>
    public int Number { get; set; }

    /// <summary>
    /// Детали подключения модуля (IP-адрес, COM-порт и т. д.).
    /// </summary>
    public object ConnectionDetails { get; set; }

    /// <summary>
    /// Тип устройства, всегда RelaySwitchModule.
    /// </summary>
    public DeviceEnum.DeviceType DeviceType => DeviceEnum.DeviceType.RelaySwitchModule;

    /// <summary>
    /// Метод инициализации модуля.
    /// </summary>
    /// <returns>Возвращает true, если инициализация прошла успешно.</returns>
    public Task<bool> Initialize() => Task.FromResult(true);
  }
}
