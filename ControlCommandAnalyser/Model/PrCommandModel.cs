using AppConfiguration.Error.Translation;
using ControlCommandAnalyser.Model.Chains;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities.Models;

namespace ControlCommandAnalyser.Model
{
   [AllowedKeys(ControlCommandAnalyser.AlgorithmKey.К, /*ControlCommandAnalyser.AlgorithmKey.С, ControlCommandAnalyser.AlgorithmKey.П, ControlCommandAnalyser.AlgorithmKey.И,
    ControlCommandAnalyser.AlgorithmKey.Г,*/ ControlCommandAnalyser.AlgorithmKey.Т1)]
  public class PrCommandModel : BaseCommandModel, IError, IHasScheme
  {
    public override string Mnemonic => "ПР";

    /// <summary>
    /// Нижняя граница значеня сопротивления (например, "100<МОм")
    /// </summary>
    public string? LowerLimitResistance { get; set; }

    /// <summary>
    /// Верхняя граница значения сопротивления (например, "МОм<100").
    /// </summary>
    public string? HigherLimitResistance { get; set; }

    /// <summary>
    /// Список точек измерения.
    /// </summary>
    public SchemeModel Scheme { get; set; }

    /// <summary>
    /// Остаток строки с нераспознанными параметрами.
    /// </summary>
    public string? UnparsedParameters { get; set; }

    /// <summary>
    /// Ошибки связанные с замыканием точек.
    /// </summary>
    public override IPointError PointErrors => new PrErrors();
  }
}
