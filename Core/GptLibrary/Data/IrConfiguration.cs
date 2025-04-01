namespace Core.GptLibrary.Data
{
  /// <summary>
  /// Класс для хранения конфигурации IR.
  /// </summary>
  public class IrConfiguration
  {
    /// <summary>
    /// Напряжение в вольтах (В).
    /// </summary>
    public double Voltage { get; set; }

    /// <summary>
    /// Высокий предел сопротивления в гигаомах (ГОм).
    /// </summary>
    public double HighResistanceLimit { get; set; }

    /// <summary>
    /// Низкий предел сопротивления в гигаомах (ГОм).
    /// </summary>
    public double LowResistanceLimit { get; set; }

    /// <summary>
    /// Время теста в секундах.
    /// </summary>
    public double TestTime { get; set; }

    /// <summary>
    /// Смещение в гигаомах (ГОм).
    /// </summary>
    public double Offset { get; set; }
  }
}
