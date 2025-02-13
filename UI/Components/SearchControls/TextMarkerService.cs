using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using System.Windows;
using System.Windows.Media;
using UI.Controls.TextEditor;

namespace UI.Components.SearchControls
{
  public class TextMarkerService : IBackgroundRenderer, IVisualLineTransformer
  {
    private readonly TextSegmentCollection<TextMarker> markers;
    private readonly TextEditor textEditor;

    public TextMarkerService(TextDocument document, TextEditor textEditor)
    {
      markers = new TextSegmentCollection<TextMarker>(document);
      this.textEditor = textEditor;
    }

    public IEnumerable<TextMarker> TextMarkers => markers;

    public KnownLayer Layer => KnownLayer.Selection;  // Указывает слой рендера для подсветки

    public void Draw(TextView textView, DrawingContext drawingContext)
    {
      if (textView == null || drawingContext == null)
        return;

      textView.EnsureVisualLines();

      foreach (var marker in markers)
      {
        foreach (var rect in BackgroundGeometryBuilder.GetRectsForSegment(textView, marker))
        {
          var geometry = new RectangleGeometry(rect);
          drawingContext.DrawGeometry(new SolidColorBrush(marker.BackgroundColor), null, geometry);
        }
      }
    }

    public void Transform(ITextRunConstructionContext context, IList<VisualLineElement> elements)
    {
      if (context == null || elements == null)
        return;

      // Получаем текущее выделение
      var selection = textEditor.TextArea.Selection;
      if (selection.IsEmpty)
        return;

      int selectionStart = selection.Segments.Min(s => s.StartOffset);
      int selectionEnd = selection.Segments.Max(s => s.EndOffset);

      // Начальный offset текущей строки
      int lineStartOffset = context.VisualLine.FirstDocumentLine.Offset;

      foreach (var marker in markers.FindOverlappingSegments(lineStartOffset,
          context.VisualLine.LastDocumentLine.EndOffset - lineStartOffset))
      {
        foreach (var element in elements)
        {
          // Вычисляем реальный offset элемента в документе
          int elementStart = lineStartOffset + element.VisualColumn;
          int elementEnd = elementStart + element.DocumentLength;

          // Проверяем, попадает ли элемент в выделенный диапазон
          if (elementEnd > selectionStart && elementStart < selectionEnd)
          {
            if (marker.ForegroundColor != null)
            {
              element.TextRunProperties.SetForegroundBrush(new SolidColorBrush(marker.ForegroundColor.Value));
            }
          }
        }
      }
    }

    /// <summary>
    /// Создание нового маркера для подсветки текста
    /// </summary>
    /// <param name="startOffset">Стартовая позиция.</param>
    /// <param name="length">Длина подствеки.</param>
    /// <returns>Подстветку текста необходимой длины.</returns>
    public TextMarker Create(int startOffset, int length)
    {
      var marker = new TextMarker(startOffset, length);
      markers.Add(marker);
      return marker;
    }

    /// <summary>
    /// Очистка подсветки.
    /// </summary>
    public void RemoveAll()
    {
      markers.Clear();
      textEditor.TextArea.TextView.Redraw();
    }
  }
}
