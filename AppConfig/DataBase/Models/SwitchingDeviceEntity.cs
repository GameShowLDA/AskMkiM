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
  /// Класс, представляющий сущность устройства коммутации.
  /// </summary>
  public class SwitchingDeviceEntity : ISwitchingDevice
  {
    public int Id { get; set; }
    /// <summary>
    /// Номер менеджера шасси, к которому подключено устройство коммутации.
    /// </summary>
    public int NumberChassis { get; set; }

    /// <summary>
    /// Название устройства коммутации.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Описание устройства коммутации.
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Уникальный номер устройства.
    /// </summary>
    public int Number { get; set; }

    /// <summary>
    /// Детали подключения устройства (IP-адрес, COM-порт и т. д.).
    /// </summary>
    public string ConnectionDetails { get; set; }

    /// <summary>
    /// Тип устройства, всегда SwitchingDevice.
    /// </summary>
    public DeviceEnum.DeviceType DeviceType => DeviceEnum.DeviceType.SwitchingDevice;

    /// <summary>
    /// Метод инициализации устройства коммутации.
    /// </summary>
    /// <returns>Возвращает true, если инициализация прошла успешно.</returns>
    public Task<bool> Initialize() => Task.FromResult(true);
  }
}
