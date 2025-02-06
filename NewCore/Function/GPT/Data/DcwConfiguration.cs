namespace NewCore.Function.GPT.Data
{
  /// <summary>
  /// Класс для хранения конфигурации ACW.
  /// </summary>
  public class DcwConfiguration
  {
    public double Voltage { get; set; } // Напряжение (кВ)
    public double HighCurrentLimit { get; set; } // Высокий предел тока (мА)
    public double LowCurrentLimit { get; set; } // Низкий предел тока (мА)
    public double TestTime { get; set; } // Время теста (сек)
    public double Offset { get; set; } // Смещение (мА)

    public double ArcCurrent { get; set; }
  }
}
