namespace NewCore.Function.GPT.Data
{
  /// <summary>
  /// Класс для хранения конфигурации IR.
  /// </summary>
  public class IrConfiguration
  {
    public double Voltage { get; set; } // Напряжение (в В)
    public double HighResistanceLimit { get; set; } // Высокий предел сопротивления (в ГОм)
    public double LowResistanceLimit { get; set; } // Низкий предел сопротивления (в ГОм)
    public double TestTime { get; set; } // Время теста (в секундах)
    public double Offset { get; set; } // Смещение (в ГОм)
  }
}
