using Ask.Core.Services.Extensions;
using Ask.Core.Shared.DTO.Executor;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;

namespace Ask.Engine.ControlCommandAnalyser.Model
{
  /// <summary>
  /// Модель команды УП (условный переход).
  /// </summary>
  public class OcCommandModel : BaseCommandModel
  {
    public override string Mnemonic => EnumExtensions.GetDisplayOrganizationalInfo(OrganizationalComands.OC).DisplayName;
  }
}
