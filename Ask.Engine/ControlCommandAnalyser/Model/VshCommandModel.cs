using Ask.Core.Services.Extensions;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;

namespace Ask.Engine.ControlCommandAnalyser.Model
{
  public class VshCommandModel : BaseCommandModel
  {
    public override string Mnemonic => EnumExtensions.GetDisplayOrganizationalInfo(OrganizationalComands.VSH).DisplayName;

    /// <summary>
    /// Структура шин стойки коммутации.
    /// </summary>
    public Dictionary<BusStructureEnum.Type, List<int?>> BusStructure { get; set; }
  }
}
