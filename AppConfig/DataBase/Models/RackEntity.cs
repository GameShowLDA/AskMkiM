using NewCore.Base.Interface.Additionally;
using NewCore.Base.Interface.Main;
using NewCore.Enum;

namespace AppConfig.DataBase.Models
{
  /// <summary>
  /// Класс, представляющий сущность стойка коммутационная.
  /// </summary>
  public class RackEntity : IRack, IHeadUnit
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
    /// Номер менеджера шасси.
    /// </summary>
    public int NumberChassis { get; set; }

    /// <summary>
    /// Данные соединения (IP-адрес или COM-порт).
    /// </summary>
    public string ConnectionDetails { get; set; }

    /// <summary>
    /// Тип устройства (стойка СКМ).
    /// </summary>
    public DeviceEnum.DeviceType DeviceType => DeviceEnum.DeviceType.Rack;

    public string DeviceClass { get; set; }


    public Task<bool> Initialize() => Task.FromResult(true);
  }
}
