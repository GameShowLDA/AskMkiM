using ControlCommandAnalyser.Model.Chains;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlCommandAnalyser.Model
{
  /// <summary>
  /// Модель для команды ИЕ (измерение емкости).
  /// </summary>
  [AllowedKeys(ControlCommandAnalyser.AlgorithmKey.Д)]
  public class IeCommandModel : BaseCommandModel, IHasScheme
  {

    public override string Mnemonic => "ИЕ";

    /// <summary>
    /// Нижняя граница значеня элктрической ёмкости (например, "100<МОм")
    /// </summary>
    public string? LowerLimitCapacity { get; set; }

    /// <summary>
    /// Верхняя граница элктрической ёмкости (например, "МОм<100").
    /// </summary>
    public string? HigherLimitCapacity { get; set; }

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
