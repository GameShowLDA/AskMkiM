using Ask.Core.Services.Extensions;
using Ask.Core.Shared.DTO.Executor;
using Ask.Core.Shared.Interfaces.ExecutionInterfaces;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;

namespace Ask.Engine.ControlCommandAnalyser.Model
{
  public class VshCommandModel : BaseCommandModel, IHasRackStructure
  {
    public override string Mnemonic => EnumExtensions.GetDisplayOrganizationalInfo(OrganizationalComands.VSH).DisplayName;

    public Dictionary<BusStructureEnum.Type, List<int?>> BusStructure { get; set; }
  }
}
