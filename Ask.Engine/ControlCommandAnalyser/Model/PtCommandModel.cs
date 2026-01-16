using Ask.Core.Services.Extensions;
using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
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
  public class PtCommandModel : BaseCommandModel
  {
    public override string Mnemonic => EnumExtensions.GetDisplayOrganizationalInfo(OrganizationalComands.PT).DisplayName;

    /// <summary>
    /// Значение времени (например, "1c").
    /// </summary>
    public string? TimeSource { get; set; }
    public double? Time { get; set; }

    /// <summary>
    /// Список точек измерения.
    /// </summary>
    public Dictionary<string, List<(string, string)>> BusPointsDictionary { get; set; } = new();

    /// <summary>
    /// Остаток строки с нераспознанными параметрами.
    /// </summary>
    public string? UnparsedParameters { get; set; }
  }
}
