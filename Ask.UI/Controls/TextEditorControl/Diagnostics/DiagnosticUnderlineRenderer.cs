using Ask.Engine.ControlCommandAnalyser;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using System.Windows;
using System.Windows.Media;

namespace Ask.UI.Controls.TextEditorControl.Diagnostics;

/// <summary>A separate diagnostic layer, independent of search and execution markers.</summary>
internal sealed class DiagnosticUnderlineRenderer : IBackgroundRenderer
{
  private static readonly Pen ErrorPen = CreatePen(Colors.Red);
  private static readonly Pen WarningPen = CreatePen(Colors.Gold);
  private readonly TextView _view;
  private TextSegmentCollection<DiagnosticSegment> _segments = new();

  public DiagnosticUnderlineRenderer(TextView view) => _view = view;
  public KnownLayer Layer => KnownLayer.Selection;
  internal IReadOnlyList<SourceDiagnostic> Diagnostics { get; private set; } = Array.Empty<SourceDiagnostic>();

  internal void SetDiagnostics(IReadOnlyList<SourceDiagnostic> diagnostics)
  {
    Diagnostics = diagnostics;
    _segments = new TextSegmentCollection<DiagnosticSegment>();
    foreach (var diagnostic in diagnostics)
      _segments.Add(new DiagnosticSegment(diagnostic));
    _view.InvalidateLayer(Layer);
  }

  internal IReadOnlyList<SourceDiagnostic> GetDiagnosticsAt(int offset) =>
    _segments.FindSegmentsContaining(offset)
      .Where(s => offset < s.EndOffset)
      .Select(s => s.Diagnostic).OrderBy(d => d.IsWarning).ToArray();

  internal IReadOnlyList<SourceDiagnostic> GetDiagnosticsAt(Point point, out Rect anchor)
  {
    anchor = Rect.Empty;
    if (!_view.VisualLinesValid || _view.VisualLines.Count == 0
      || !new Rect(_view.RenderSize).Contains(point)) return Array.Empty<SourceDiagnostic>();

    int start = _view.VisualLines[0].FirstDocumentLine.Offset;
    int end = _view.VisualLines[^1].LastDocumentLine.EndOffset;
    var matches = new List<SourceDiagnostic>();
    foreach (var segment in _segments.FindOverlappingSegments(start, end - start))
    {
      foreach (var rect in BackgroundGeometryBuilder.GetRectsForSegment(_view, segment))
      {
        // Hit the actual glyph/underline rectangle, including the last character.
        // A nearest-caret lookup can round that character to the end of the span.
        if (!rect.Contains(point)) continue;
        var visible = Rect.Intersect(rect, new Rect(_view.RenderSize));
        if (anchor.IsEmpty) anchor = visible;
        else anchor.Union(visible);
        matches.Add(segment.Diagnostic);
        break;
      }
    }
    return matches.OrderBy(d => d.IsWarning).ToArray();
  }

  public void Draw(TextView textView, DrawingContext drawingContext)
  {
    if (!textView.VisualLinesValid || textView.VisualLines.Count == 0) return;
    int start = textView.VisualLines[0].FirstDocumentLine.Offset;
    int end = textView.VisualLines[^1].LastDocumentLine.EndOffset;

    // Draw errors last so an overlapping warning cannot hide a blocking error.
    foreach (var segment in _segments.FindOverlappingSegments(start, end - start)
      .OrderByDescending(s => s.Diagnostic.IsWarning))
    {
      foreach (var rect in BackgroundGeometryBuilder.GetRectsForSegment(textView, segment))
      {
        double left = Math.Max(0, rect.Left);
        double right = Math.Min(textView.ActualWidth, rect.Right);
        if (right <= left) continue;
        var geometry = new StreamGeometry();
        using (var context = geometry.Open())
        {
          double bottom = rect.Bottom - 1;
          context.BeginFigure(new Point(left, bottom), false, false);
          bool up = true;
          for (double x = left + 2; x < right; x += 2)
          {
            context.LineTo(new Point(x, bottom - (up ? 2 : 0)), true, false);
            up = !up;
          }
          context.LineTo(new Point(right, bottom), true, false);
        }
        geometry.Freeze();
        drawingContext.DrawGeometry(null, segment.Diagnostic.IsWarning ? WarningPen : ErrorPen, geometry);
      }
    }
  }

  private static Pen CreatePen(Color color)
  {
    var pen = new Pen(new SolidColorBrush(color), 1.2);
    pen.Freeze();
    return pen;
  }

  private sealed class DiagnosticSegment : TextSegment
  {
    public SourceDiagnostic Diagnostic { get; }

    // TextSegment offsets belong to a snapshot and are cleared immediately on edits.
    public DiagnosticSegment(SourceDiagnostic diagnostic)
    {
      Diagnostic = diagnostic;
      StartOffset = diagnostic.Offset;
      Length = diagnostic.Length;
    }
  }
}
