using Ask.Core.Shared.Metadata.View.EditorHost.TextEditor;
using ICSharpCode.AvalonEdit.Document;

namespace Ask.UI.Controls.TextEditorControl
{
  /// <summary>
  /// Реализация отслеживаемого текстового сегмента на основе anchor’ов AvalonEdit.
  ///
  /// Представляет диапазон документа, который автоматически смещается при изменении текста.
  /// Используется как адаптер между <see cref="ITextSegment"/> и механизмом
  /// <see cref="TextAnchor"/> редактора AvalonEdit.
  ///
  /// Диапазон определяется двумя якорями (начало и конец), поэтому:
  ///  • корректно переживает вставку/удаление текста
  ///  • всегда возвращает актуальные координаты
  ///  • не хранит статические offset’ы
  ///
  /// Применяется для защищённых областей, диагностик и логических привязок.
  /// </summary>
  internal sealed class AvalonAnchorSegment : ITextSegment
  {
    private readonly TextAnchor _start;
    private readonly TextAnchor _end;

    /// <summary>
    /// Создаёт отслеживаемый сегмент по двум anchor’ам документа.
    /// </summary>
    /// <param name="start">Якорь начала диапазона.</param>
    /// <param name="end">Якорь конца диапазона.</param>
    public AvalonAnchorSegment(TextAnchor start, TextAnchor end)
    {
      _start = start ?? throw new ArgumentNullException(nameof(start));
      _end = end ?? throw new ArgumentNullException(nameof(end));
    }

    /// <summary>
    /// Текущий offset начала диапазона (0-based).
    /// Значение изменяется при редактировании документа.
    /// </summary>
    public int Offset => Math.Min(_start.Offset, _end.Offset);

    /// <summary>
    /// Текущий offset конца диапазона.
    /// </summary>
    public int EndOffset => Math.Max(_start.Offset, _end.Offset);

    /// <summary>
    /// Текущая длина диапазона.
    /// </summary>
    public int Length => EndOffset - Offset;

    /// <summary>
    /// Преобразует сегмент в нативный сегмент AvalonEdit.
    /// Используется только UI-слоем.
    /// </summary>
    public ISegment ToAvalonSegment()
    {
      return new AvalonSimpleSegment(Offset, Length);
    }
  }
}
