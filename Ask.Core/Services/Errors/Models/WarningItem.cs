namespace Ask.Core.Services.Errors.Models
{
  /// <summary>
  /// Представляет элемент ошибки, возникшей при выполнении команды или измерения.
  /// Содержит сведения о строке исходного кода, команде, описании и результате измерения.
  /// </summary>
  public class WarningItem : IDisplayIssue
  {
    /// <summary>
    /// Номер строки в исходном файле, где вызвано предупреждение.
    /// </summary>
    public int SourceLineNumber { get; set; }

    /// <summary>
    /// Отформатированный номер строки для отображения пользователю.
    /// Может отличаться от исходного, если выполнялось форматирование или объединение строк.
    /// </summary>
    public int FormattedLineNumber { get; set; }

    /// <summary>
    /// Команда, в которой возникло предупреждение.
    /// </summary>
    public string Command { get; set; } = string.Empty;

    /// <summary>
    /// Текстовое описание предупреждение или пояснение к нему.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Результат измерения, при котором зафиксировано предупреждение (если применимо).
    /// </summary>
    public string MeasureResult { get; set; } = string.Empty;

    /// <summary>
    /// Код предупреждения, определяющий его тип и причину.
    /// Использует перечисление <see cref="WarningCode"/>.
    /// </summary>
    public WarningCode? Code { get; set; }

    public string? CodeString => Code?.ToString();

    public bool IsWarning => true;
  }
}
