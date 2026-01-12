using Ask.Core.Services.Extensions;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Engine.ControlCommandAnalyser.Attributes;

namespace Ask.Engine.ControlCommandAnalyser.Model
{
  /// <summary>
  /// Тип команды ЦУ: информация, вопрос, переход (вопрос с условием).
  /// </summary>
  public enum CuCommandType
  {
    /// <summary>Простое информационное сообщение.</summary>
    Information,

    /// <summary>Вопрос (есть "?" или "??", но не следует команда УП).</summary>
    Question,

    /// <summary>Вопрос + переход (за знаком вопроса сразу идёт команда УП).</summary>
    QuestionWithConditionalJump
  }

  /// <summary>
  /// Модель команды ЦУ (сообщение оператору).
  /// </summary>
  [AllowedKeys(Ask.Core.Shared.Metadata.Enums.TranslationEnums.AlgorithmKey.Д)]
  public class CuCommandModel : BaseCommandModel
  {
    public override string Mnemonic => EnumExtensions.GetDisplayOrganizationalInfo(OrganizationalComands.CU).DisplayName;

    /// <summary>
    /// Тип команды ЦУ (информация, вопрос, переход).
    /// </summary>
    public CuCommandType CuType { get; set; } = CuCommandType.Information;

    /// <summary>
    /// Основной текст сообщения (без ключей).
    /// </summary>
    public string MessageText { get; set; }

    /// <summary>
    /// Флаг — требуется ли документирование ($DOC).
    /// </summary>
    public bool IsDocument { get; set; }

    /// <summary>
    /// Если команда — вопрос с переходом, сюда записывается номер команды для УП.
    /// </summary>
    public string? ConditionalJumpTarget { get; set; }
  }
}
