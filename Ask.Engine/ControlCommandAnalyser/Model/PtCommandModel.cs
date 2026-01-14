using Ask.Core.Services.Extensions;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Engine.ControlCommandAnalyser.Attributes;
using Ask.Engine.ControlCommandAnalyser.Model.Chains;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ask.Engine.ControlCommandAnalyser.Model
{
  [AllowedKeys(Ask.Core.Shared.Metadata.Enums.TranslationEnums.AlgorithmKey.Б, Ask.Core.Shared.Metadata.Enums.TranslationEnums.AlgorithmKey.П, Ask.Core.Shared.Metadata.Enums.TranslationEnums.AlgorithmKey.С)]
  public class PtCommandModel : BaseCommandModel, IHasScheme
  {
    public override string Mnemonic => EnumExtensions.GetDisplayInfo(OrganizationalComands.PT).DisplayName;

    /// <summary>
    /// Значение времени (например, "1c").
    /// </summary>
    public string? TimeSource { get; set; }
    public double? Time { get; set; }

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
