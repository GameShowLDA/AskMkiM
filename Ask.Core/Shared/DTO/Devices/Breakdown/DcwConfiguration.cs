namespace Ask.Core.Shared.DTO.Devices.Breakdown
{
  /// <summary>
  /// Класс для хранения конфигурации DCW (постоянный ток высокого напряжения).
  /// </summary>
  public class DcwConfiguration
  {
    /// <summary>
    /// Напряжение теста DCW (в кВ).
    /// </summary>
    public double Voltage { get; set; }

    /// <summary>
    /// Верхний предел тока DCW (в мА).
    /// </summary>
    public double HighCurrentLimit { get; set; }

    /// <summary>
    /// Нижний предел тока DCW (в мА).
    /// </summary>
    public double LowCurrentLimit { get; set; }

    /// <summary>
    /// Время теста DCW (в секундах).
    /// </summary>
    public double TestTime { get; set; }

    /// <summary>
    /// Время теста DCW (в секундах).
    /// </summary>
    public double RampTime { get; set; }

    /// <summary>
    /// Смещение измерения DCW (в мА).
    /// </summary>
    public double Offset { get; set; }

    /// <summary>
    /// Ток дугового пробоя DCW (в мА).
    /// </summary>
    public double ArcCurrent { get; set; }
  }
}
