using Ask.Core.Services.Extensions;
using Ask.Core.Shared.DTO.Executor;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;

namespace Ask.Engine.ControlCommandAnalyser.Model
{
  public class CpCommandModel : BaseCommandModel
  {
    /// <inheritdoc />
    public override string Mnemonic => EnumExtensions.GetCommandOrganizationalInfo(OrganizationalComands.CP).DisplayName;
  }
}
