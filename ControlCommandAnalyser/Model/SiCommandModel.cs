using System.Collections.Generic;
using ControlCommandAnalyser.Model;
using ControlCommandAnalyser.Parser.Common;

namespace ControlCommandAnalyser.Model
{

  /// <summary>
  /// Модель для команды СИ (сопротивление изоляции).
  /// </summary>
  [AllowedKeys(ControlCommandAnalyser.AlgorithmKey.К)]
  public class SiCommandModel : BaseCommandModel, IHasPoints
  {
    /// <summary>
    /// Значение напряжения (например, "100В", "1кВ").
    /// </summary>
    public string? Voltage { get; set; }

    /// <summary>
    /// Значение сопротивления (например, "100<МОм").
    /// </summary>
    public string? Resistance { get; set; }

    /// <summary>
    /// Значение времени (например, "1c").
    /// </summary>
    public string? Time { get; set; }

    /// <summary>
    /// Список точек измерения.
    /// </summary>
    public List<string> Points { get; set; } = new();

    /// <summary>
    /// Остаток строки с нераспознанными параметрами.
    /// </summary>
    public string? UnparsedParameters { get; set; }
  }
}
