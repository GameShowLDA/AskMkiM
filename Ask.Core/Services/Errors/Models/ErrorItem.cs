namespace Ask.Core.Services.Errors.Models
{
  /// <summary>
  /// Представляет элемент ошибки, возникшей при выполнении команды или измерения.
  /// Содержит сведения о строке исходного кода, команде, описании и результате измерения.
  /// </summary>
  public class ErrorItem : IDisplayIssue
  {
    /// <summary>
    /// Номер строки в исходном файле, где произошла ошибка.
    /// </summary>
    public int SourceLineNumber { get; set; }

    /// <summary>
    /// Отформатированный номер строки для отображения пользователю.
    /// Может отличаться от исходного, если выполнялось форматирование или объединение строк.
    /// </summary>
    public int FormattedLineNumber { get; set; }

    /// <summary>
    /// Команда, в которой возникла ошибка.
    /// </summary>
    public string Command { get; set; } = string.Empty;

    /// <summary>
    /// Текстовое описание ошибки или пояснение к ней.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Результат измерения, при котором зафиксирована ошибка (если применимо).
    /// </summary>
    public string MeasureResult { get; set; } = string.Empty;

    /// <summary>
    /// Отладочная информация.
    /// </summary>
    public string DebugInfo { get; set; } = string.Empty;

    /// <summary>
    /// Код ошибки, определяющий её тип и причину.
    /// Использует перечисление <see cref="ErrorCode"/>.
    /// </summary>
    public ErrorCode? Code { get; set; }

    public string? CodeString => Code?.ToString();

    public bool IsWarning => false;
  }
}
