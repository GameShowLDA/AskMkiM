using System.Collections.Generic;
using AppConfiguration.Error.Translation;
using ControlCommandAnalyser.Model;
using ControlCommandAnalyser.Parser.Common;

namespace ControlCommandAnalyser.Model
{

  /// <summary>
  /// Модель для команды СИ (сопротивление изоляции).
  /// </summary>
  [AllowedKeys(ControlCommandAnalyser.AlgorithmKey.К, /*ControlCommandAnalyser.AlgorithmKey.С, ControlCommandAnalyser.AlgorithmKey.П, ControlCommandAnalyser.AlgorithmKey.И,*/
    ControlCommandAnalyser.AlgorithmKey.Г, ControlCommandAnalyser.AlgorithmKey.Т1)]
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

    public override IPointError PointErrors => new SiErrors();
  }
}
