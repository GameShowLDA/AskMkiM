using NewCore.Enum;
using NewCore.Interface;

namespace AppConfig.DataBase.Models
{
  /// <summary>
  /// Класс, представляющий сущность точного измерителя.
  /// </summary>
  public class PrecisionMeterEntity : IPrecisionMeter
  {
    public int Id { get; set; }
    /// <summary>
    /// Номер менеджера шасси, к которому подключен точный измеритель.
    /// </summary>
    public int NumberChassis { get; set; }

    /// <summary>
    /// Название точного измерителя.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Описание точного измерителя.
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
    /// Тип устройства, всегда PrecisionMeter.
    /// </summary>
    public DeviceEnum.DeviceType DeviceType => DeviceEnum.DeviceType.PrecisionMeter;

    /// <summary>
    /// Метод инициализации точного измерителя.
    /// </summary>
    /// <returns>Возвращает true, если инициализация прошла успешно.</returns>
    public Task<bool> Initialize() => Task.FromResult(true);
  }
}
