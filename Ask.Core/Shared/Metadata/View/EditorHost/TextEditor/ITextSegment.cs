namespace Ask.Core.Shared.Metadata.View.EditorHost.TextEditor
{
  /// <summary>
  /// Абстрактный диапазон текста документа.
  /// Не зависит от конкретной UI-реализации редактора.
  /// </summary>
  public interface ITextSegment
  {
    /// <summary>Начальное смещение в документе.</summary>
    int Offset { get; }

    /// <summary>Длина сегмента.</summary>
    int Length { get; }

    /// <summary>Конечное смещение (Offset + Length).</summary>
    int EndOffset { get; }
  }
}
