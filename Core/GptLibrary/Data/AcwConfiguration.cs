namespace Core.GptLibrary.Data
{
  /// <summary>
  /// Класс для хранения конфигурации ACW.
  /// </summary>
  public class AcwConfiguration
  {
    /// <summary>
    /// Напряжение в киловольтах (кВ).
    /// </summary>
    public double Voltage { get; set; }

    /// <summary>
    /// Высокий предел тока в миллиамперах (мА).
    /// </summary>
    public double HighCurrentLimit { get; set; }

    /// <summary>
    /// Низкий предел тока в миллиамперах (мА).
    /// </summary>
    public double LowCurrentLimit { get; set; }

    /// <summary>
    /// Время теста в секундах.
    /// </summary>
    public double TestTime { get; set; }

    /// <summary>
    /// Частота в герцах (Гц).
    /// </summary>
    public int Frequency { get; set; }

    /// <summary>
    /// Смещение в миллиамперах (мА).
    /// </summary>
    public double Offset { get; set; }

    /// <summary>
    /// Ток дуги.
    /// </summary>
    public double ArcCurrent { get; set; }
  }
}
