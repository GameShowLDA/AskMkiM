using Ask.Core.Services.Extensions;
using Ask.Core.Shared.DTO.Executor;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;

namespace Ask.Engine.ControlCommandAnalyser.Model
{
  /// <summary>
  /// Модель команды ОС (общий сброс).
  /// </summary>
  public class OcCommandModel : BaseCommandModel
  {
    public override string Mnemonic => EnumExtensions.GetDisplayOrganizationalInfo(OrganizationalComands.OC).DisplayName;
  }
}
