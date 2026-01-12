namespace Ask.Core.Shared.DTO.Devices.Breakdown
{
  /// <summary>
  /// Класс для хранения конфигурации ACW (переменный ток высокого напряжения).
  /// </summary>
  public class AcwConfiguration
  {
    /// <summary>
    /// Напряжение теста ACW (в кВ).
    /// </summary>
    public double Voltage { get; set; }

    /// <summary>
    /// Верхний предел тока ACW (в мА).
    /// </summary>
    public double HighCurrentLimit { get; set; }

    /// <summary>
    /// Нижний предел тока ACW (в мА).
    /// </summary>
    public double LowCurrentLimit { get; set; }

    /// <summary>
    /// Время теста ACW (в секундах).
    /// </summary>
    public double TestTime { get; set; }

    /// <summary>
    /// Время теста ACW (в секундах).
    /// </summary>
    public double RampTime { get; set; }

    /// <summary>
    /// Частота теста ACW (в Гц). Должна быть 50 или 60 Гц.
    /// </summary>
    public int Frequency { get; set; }

    /// <summary>
    /// Смещение измерения ACW (в мА).
    /// </summary>
    public double Offset { get; set; }

    /// <summary>
    /// Ток дугового пробоя ACW (в мА).
    /// </summary>
    public double ArcCurrent { get; set; }
  }
}
