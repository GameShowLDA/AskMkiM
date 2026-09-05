namespace UI.Controls.TextEditorControl.Syntax
{
  /// <summary>
  /// Описывает участок исходного документа, который нужно подчеркнуть
  /// для конкретной ошибки или предупреждения.
  /// </summary>
  internal readonly struct CommandIssueSpan
  {
    /// <summary>
    /// Создаёт диапазон диагностики в координатах AvalonEdit.
    /// </summary>
    /// <param name="startOffset">Абсолютное смещение начала диапазона в документе.</param>
    /// <param name="length">Длина диапазона в символах.</param>
    /// <param name="lineNumber">Номер строки диапазона в исходном документе.</param>
    /// <param name="columnNumber">Номер колонки диапазона в исходном документе.</param>
    public CommandIssueSpan(
      int startOffset,
      int length,
      int lineNumber,
      int columnNumber)
    {
      StartOffset = startOffset;
      Length = length;
      LineNumber = lineNumber;
      ColumnNumber = columnNumber;
    }

    /// <summary>
    /// Абсолютное смещение начала диапазона в документе.
    /// </summary>
    public int StartOffset { get; }

    /// <summary>
    /// Длина диапазона в символах.
    /// </summary>
    public int Length { get; }

    /// <summary>
    /// Номер строки диапазона в исходном документе.
    /// </summary>
    public int LineNumber { get; }

    /// <summary>
    /// Номер колонки диапазона в исходном документе.
    /// </summary>
    public int ColumnNumber { get; }
  }
}
