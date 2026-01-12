namespace Ask.Core.Shared.DTO.Devices.Breakdown
{
  /// <summary>
  /// Класс для хранения конфигурации режима IR (изоляционное сопротивление).
  /// </summary>
  public class IrConfiguration
  {
    /// <summary>
    /// Напряжение теста IR (в Вольтах).
    /// </summary>
    public double Voltage { get; set; }

    /// <summary>
    /// Верхний предел измеряемого сопротивления (в ГОм).
    /// </summary>
    public double HighResistanceLimit { get; set; }

    /// <summary>
    /// Нижний предел измеряемого сопротивления (в ГОм).
    /// </summary>
    public double LowResistanceLimit { get; set; }

    /// <summary>
    /// Время теста IR (в секундах).
    /// </summary>
    public double TestTime { get; set; }

    /// <summary>
    /// Время нарастания IR (в секундах).
    /// </summary>
    public double RampTime { get; set; }

    /// <summary>
    /// Смещение измерения IR (в ГОм).
    /// </summary>
    public double Offset { get; set; }
  }
}
