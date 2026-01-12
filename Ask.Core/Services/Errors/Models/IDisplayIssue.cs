namespace Ask.Core.Services.Errors.Models
{
  /// <summary>
  /// Общий интерфейс для элементов диагностики (ошибок и предупреждений),
  /// поддерживающий единый набор свойств для отображения в UI.
  /// </summary>
  public interface IDisplayIssue
  {
    /// <summary>
    /// Номер строки в исходном файле.
    /// </summary>
    int SourceLineNumber { get; }

    /// <summary>
    /// Номер строки для отображения пользователю.
    /// </summary>
    int FormattedLineNumber { get; }

    /// <summary>
    /// Команда, в которой возникла проблема.
    /// </summary>
    string Command { get; }

    /// <summary>
    /// Текст описания ошибки/предупреждения.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Результат измерения.
    /// </summary>
    string MeasureResult { get; }

    /// <summary>
    /// Код ошибки или предупреждения, приведённый к строке.
    /// Используется для универсального отображения в UI.
    /// </summary>
    string? CodeString { get; }

    /// <summary>
    /// Признак того, что это предупреждение (а не ошибка).
    /// </summary>
    bool IsWarning { get; }
  }
}
