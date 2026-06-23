using Ask.Core.Services.Errors.Models;

namespace Ask.Core.Services.Errors.Translation
{
  /// <summary>
  /// Содержит шаблоны ошибок, возникающих при парсинге и анализе команд УП (условный переход).
  /// </summary>
  public static class UpErrors
  {
    /// <summary>
    /// Ошибка: не указана или некорректна метка перехода в команде УП.
    /// </summary>
    public static ErrorItem MissingOrInvalidLabel(int lineNumber, string command) => new()
    {
      SourceLineNumber = lineNumber,
      Command = command,
      Code = ErrorCode.Up_MissingOrInvalidUpLabel,
      Description = "В команде УП не указана или указана некорректная метка перехода."
    };
    /// <summary>
    /// Ошибка: в команде УП указана ссылка на саму себя.
    /// </summary>
    public static ErrorItem SelfReferenceJump(int lineNumber, string command) => new()
    {
      SourceLineNumber = lineNumber,
      Command = command,
      Code = ErrorCode.Up_SelfReferenceJump,
      Description = "В команде УП указана ссылка на саму себя."
    };

    /// <summary>
    /// Ошибка: метка перехода не найдена среди номеров команд.
    /// </summary>
    public static ErrorItem LabelNotFound(string label, int lineNumber, string command) => new()
    {
      SourceLineNumber = lineNumber,
      Command = command,
      Code = ErrorCode.Up_UpLabelNotFound,
      Description = $"Метка перехода '{label}' не найдена среди команд."
    };

    /// <summary>
    /// Ошибка: метка перехода должна быть больше номера текущей команды УП.
    /// </summary>
    public static ErrorItem LabelLessOrEqual(string label, string current, int lineNumber, string command) => new()
    {
      SourceLineNumber = lineNumber,
      Command = command,
      Code = ErrorCode.Up_UpLabelNotFound, // Можно добавить отдельный ErrorCode, если требуется
      Description = $"Метка перехода '{label}' должна быть больше номера текущей команды ({current})."
    };

    /// <summary>
    /// Ошибка: формат метки перехода некорректен (не число).
    /// </summary>
    public static ErrorItem LabelIsNotNumber(string label, int lineNumber, string command) => new()
    {
      SourceLineNumber = lineNumber,
      Command = command,
      Code = ErrorCode.Up_MissingOrInvalidUpLabel,
      Description = $"Метка перехода '{label}' должна быть целым числом."
    };
  }
}
