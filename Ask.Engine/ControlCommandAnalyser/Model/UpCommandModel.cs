using Ask.Core.Services.Extensions;
using Ask.Core.Shared.DTO.Executor;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;

namespace Ask.Engine.ControlCommandAnalyser.Model
{
  /// <summary>
  /// Модель команды УП (условный переход).
  /// </summary>
  public class UpCommandModel : BaseCommandModel
  {
    public override string Mnemonic => EnumExtensions.GetDisplayOrganizationalInfo(OrganizationalComands.UP).DisplayName;

    /// <summary>
    /// Номер перехода (метка, на которую надо перейти).
    /// </summary>
    public string TargetLabel { get; set; }
  }
}
