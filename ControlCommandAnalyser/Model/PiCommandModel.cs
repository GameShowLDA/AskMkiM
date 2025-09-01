using ControlCommandAnalyser.Model.Chains;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlCommandAnalyser.Model
{
  public class PiCommandModel : BaseCommandModel
  {
    /// <summary>
    /// Значение напряжения (например, "100В", "1кВ").
    /// </summary>
    public string? Voltage { get; set; }

    /// <summary>
    /// Пороговое сопротивление (например, "R>100МОм").
    /// </summary>
    public string? ThresholdResistance { get; set; }

    /// <summary>
    /// Значение времени (например, "1c").
    /// </summary>
    public string? Time { get; set; }

    /// <summary>
    /// Список точек измерения.
    /// </summary>
    public SchemeModel Scheme { get; set; }

    /// <summary>
    /// Остаток строки с нераспознанными параметрами.
    /// </summary>
    public string? UnparsedParameters { get; set; }
  }
}
