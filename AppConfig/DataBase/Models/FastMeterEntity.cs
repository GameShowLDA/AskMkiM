using NewCore.Enum;
using NewCore.Interface;

namespace AppConfig.DataBase.Models
{
  /// <summary>
  /// Класс, представляющий сущность быстрого измерителя.
  /// </summary>
  public class FastMeterEntity : IFastMeter
  {
    public int Id { get; set; }
    /// <summary>
    /// Номер менеджера шасси, к которому подключен быстрый измеритель.
    /// </summary>
    public int NumberChassis { get; set; }

    /// <summary>
    /// Название быстрого измерителя.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Описание быстрого измерителя.
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Уникальный номер устройства.
    /// </summary>
    public int Number { get; set; }

    /// <summary>
    /// Детали подключения измерителя (IP-адрес, COM-порт и т. д.).
    /// </summary>
    public string ConnectionDetails { get; set; }

    /// <summary>
    /// Тип устройства, всегда FastMeter.
    /// </summary>
    public DeviceEnum.DeviceType DeviceType => DeviceEnum.DeviceType.FastMeter;

    /// <summary>
    /// Метод инициализации быстрого измерителя.
    /// </summary>
    /// <returns>Возвращает true, если инициализация прошла успешно.</returns>
    public Task<bool> Initialize() => Task.FromResult(true);
  }
}
