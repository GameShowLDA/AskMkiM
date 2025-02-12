using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace UI.Components.SearchControls
{
  public class TextMarkerService : IBackgroundRenderer, IVisualLineTransformer
  {
    private readonly TextSegmentCollection<TextMarker> markers;

    public TextMarkerService(TextDocument document)
    {
      markers = new TextSegmentCollection<TextMarker>(document);
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

      foreach (var marker in markers.FindOverlappingSegments(context.VisualLine.FirstDocumentLine.Offset, context.VisualLine.LastDocumentLine.EndOffset - context.VisualLine.FirstDocumentLine.Offset))
      {
        foreach (var element in elements)
        {
          if (marker.ForegroundColor != null)
            element.TextRunProperties.SetForegroundBrush(new SolidColorBrush(marker.ForegroundColor.Value));
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
    }
  }
}
