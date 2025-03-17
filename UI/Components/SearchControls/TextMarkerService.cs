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

      // Вычисляем границы текущей строки
      int lineStartOffset = context.VisualLine.FirstDocumentLine.Offset;
      int lineEndOffset = context.VisualLine.LastDocumentLine.EndOffset;

      // Для каждого маркера, который пересекается с текущей строкой
      foreach (var marker in markers.FindOverlappingSegments(lineStartOffset, lineEndOffset - lineStartOffset))
      {
        // Для каждого элемента строки
        foreach (var element in elements)
        {
          // Определяем реальные границы элемента в документе
          int elementStart = lineStartOffset + element.VisualColumn;
          int elementEnd = elementStart + element.DocumentLength;

          // Если элемент пересекается с диапазоном маркера, применяем цвет
          if (elementEnd > marker.StartOffset && elementStart < marker.EndOffset)
          {
            if (marker.ForegroundColor != null)
            {
              element.TextRunProperties.SetForegroundBrush(new SolidColorBrush(marker.ForegroundColor.Value));
            }
            // Можно также установить фон, если нужно:
            element.TextRunProperties.SetBackgroundBrush(new SolidColorBrush(marker.BackgroundColor));
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
