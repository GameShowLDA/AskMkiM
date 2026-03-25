using Ask.Core.Shared.Metadata.View.EditorHost.TextEditor;
using ICSharpCode.AvalonEdit.Document;

namespace UI.Controls.TextEditorControl
{
  /// <summary>
  /// Методы преобразования абстрактных сегментов документа в сегменты AvalonEdit.
  ///
  /// Является частью UI-адаптера и используется для передачи диапазонов
  /// из доменной модели (<see cref="ITextSegment"/>) в механизм рендеринга
  /// редактора AvalonEdit.
  ///
  /// Не должен использоваться вне UI-слоя, так как нарушает изоляцию Core
  /// от конкретной реализации редактора.
  /// </summary>
  public static class AvalonSegmentExtensions
  {
    /// <summary>
    /// Преобразует абстрактный сегмент в нативный сегмент AvalonEdit.
    /// Если сегмент основан на anchor’ах — используется его живой диапазон,
    /// иначе создаётся статический snapshot-сегмент.
    /// </summary>
    /// <param name="segment">Абстрактный сегмент документа.</param>
    /// <returns>Сегмент, понятный AvalonEdit.</returns>
    public static ISegment ToAvalon(this ITextSegment segment)
    {
      if (segment is AvalonAnchorSegment a)
        return a.ToAvalonSegment();

      return new AvalonSimpleSegment(segment.Offset, segment.Length);
    }
  }
}
