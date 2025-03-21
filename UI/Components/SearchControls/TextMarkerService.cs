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

    //public void Transform(ITextRunConstructionContext context, IList<VisualLineElement> elements)
    //{
    //  if (context == null || elements == null)
    //    return;

    //  // Получаем абсолютные границы текущей строки
    //  int lineStartOffset = context.VisualLine.FirstDocumentLine.Offset;
    //  int lineEndOffset = context.VisualLine.LastDocumentLine.EndOffset;

    //  // Обходим все маркеры, перекрывающие текущую строку
    //  foreach (var marker in markers.FindOverlappingSegments(lineStartOffset, lineEndOffset - lineStartOffset))
    //  {
    //    foreach (var element in elements)
    //    {
    //      // Вычисляем абсолютное смещение элемента
    //      int elementStart = lineStartOffset + element.VisualColumn;
    //      int elementEnd = elementStart + element.DocumentLength;

    //      // Определяем область пересечения элемента и маркера
    //      int intersectStart = Math.Max(elementStart, marker.StartOffset);
    //      int intersectEnd = Math.Min(elementEnd, marker.EndOffset);

    //      // Если есть пересечение
    //      if (intersectEnd > intersectStart)
    //      {
    //        // Если элемент полностью покрыт маркером – задаем однородный фон
    //        if (elementStart >= marker.StartOffset && elementEnd <= marker.EndOffset)
    //        {
    //          if (marker.ForegroundColor != null)
    //          {
    //            element.TextRunProperties.SetForegroundBrush(new SolidColorBrush(marker.ForegroundColor.Value));
    //          }
    //          element.TextRunProperties.SetBackgroundBrush(new SolidColorBrush(marker.BackgroundColor));
    //        }
    //        else
    //        {
    //          // Частичное покрытие – вычисляем относительные координаты в пределах элемента
    //          double totalLength = element.DocumentLength;
    //          double relativeStart = (intersectStart - elementStart) / totalLength;
    //          double relativeEnd = (intersectEnd - elementStart) / totalLength;

    //          // Создаем линейный градиент, который будет прозрачным вне найденного диапазона
    //          var gradient = new LinearGradientBrush();
    //          gradient.StartPoint = new Point(0, 0);
    //          gradient.EndPoint = new Point(1, 0);

    //          // Добавляем градиентные остановки:
    //          // От начала до начала выделения – прозрачный
    //          gradient.GradientStops.Add(new GradientStop(Colors.Transparent, 0));
    //          gradient.GradientStops.Add(new GradientStop(Colors.Transparent, relativeStart));

    //          // От relativeStart до relativeEnd – цвет маркера
    //          gradient.GradientStops.Add(new GradientStop(marker.BackgroundColor, relativeStart));
    //          gradient.GradientStops.Add(new GradientStop(marker.BackgroundColor, relativeEnd));

    //          // От relativeEnd до конца – прозрачный
    //          gradient.GradientStops.Add(new GradientStop(Colors.Transparent, relativeEnd));
    //          gradient.GradientStops.Add(new GradientStop(Colors.Transparent, 1));

    //          element.TextRunProperties.SetBackgroundBrush(gradient);
    //          if (marker.ForegroundColor != null)
    //          {
    //            element.TextRunProperties.SetForegroundBrush(new SolidColorBrush(marker.ForegroundColor.Value));
    //          }
    //        }
    //      }
    //    }
    //  }
    //}



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
