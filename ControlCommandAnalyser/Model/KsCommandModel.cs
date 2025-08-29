using ControlCommandAnalyser.Model.Chains;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlCommandAnalyser.Model
{
  /// <summary>
  /// Модель для команды КС (контроль сопротивения).
  /// </summary>
  [AllowedKeys(ControlCommandAnalyser.AlgorithmKey.Б, ControlCommandAnalyser.AlgorithmKey.Д)]
  public class KsCommandModel : BaseCommandModel
  {
    /// <summary>
    /// Нижняя граница значеня сопротивления (например, "100<МОм")
    /// </summary>
    public string? LowerLimitResistance { get; set; }

    /// <summary>
    /// Верхняя граница значения сопротивления (например, "МОм<100").
    /// </summary>
    public string? HigherLimitResistance { get; set; }

    /// <summary>
    /// Значение времени (например, "1c").
    /// </summary>
    public string? Time { get; set; }

    /// <summary>
    /// Список точек измерения.
    /// </summary>
    public ShemeModel Sheme { get; set; }

    /// <summary>
    /// Остаток строки с нераспознанными параметрами.
    /// </summary>
    public string? UnparsedParameters { get; set; }
  }
}
