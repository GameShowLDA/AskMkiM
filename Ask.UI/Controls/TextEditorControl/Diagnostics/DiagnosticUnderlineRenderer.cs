using Ask.Engine.ControlCommandAnalyser;
using ICSharpCode.AvalonEdit.Rendering;
using System.Windows;
using System.Windows.Media;

namespace Ask.UI.Controls.TextEditorControl.Diagnostics;

/// <summary>A cached viewport layer, independent of search and execution markers.</summary>
internal sealed class DiagnosticUnderlineRenderer : IBackgroundRenderer
{
  private static readonly Pen ErrorPen = CreatePen(Colors.Red);
  private static readonly Pen WarningPen = CreatePen(Colors.Gold);
  private readonly TextView _view;
  private DiagnosticSnapshot _snapshot = DiagnosticSnapshot.Empty;
  private bool _showErrors = true, _showWarnings = true;
  private bool _geometryValid;
  private StreamGeometry? _errorGeometry, _warningGeometry;
  private readonly Dictionary<DiagnosticSnapshot.DiagnosticGroup, Rect[]> _hitRects = new();
  private IReadOnlyList<DiagnosticSnapshot.DiagnosticGroup> _lastHitGroups = Array.Empty<DiagnosticSnapshot.DiagnosticGroup>();
  private IReadOnlyList<SourceDiagnostic> _lastHitDiagnostics = Array.Empty<SourceDiagnostic>();

  public DiagnosticUnderlineRenderer(TextView view)
  {
    _view = view;
    view.VisualLinesChanged += (_, _) => Invalidate();
    view.ScrollOffsetChanged += (_, _) => Invalidate();
    view.SizeChanged += (_, _) => Invalidate();
  }

  public KnownLayer Layer => KnownLayer.Selection;

  internal void SetDiagnostics(DiagnosticSnapshot snapshot, bool showErrors, bool showWarnings)
  {
    _snapshot = snapshot;
    _showErrors = showErrors;
    _showWarnings = showWarnings;
    _lastHitGroups = Array.Empty<DiagnosticSnapshot.DiagnosticGroup>();
    _lastHitDiagnostics = Array.Empty<SourceDiagnostic>();
    Invalidate();
  }

  private void Invalidate()
  {
    _geometryValid = false;
    _errorGeometry = _warningGeometry = null;
    _hitRects.Clear();
    _view.InvalidateLayer(Layer);
  }

  private bool IsVisible(SourceDiagnostic diagnostic) => diagnostic.IsWarning ? _showWarnings : _showErrors;

  internal IReadOnlyList<SourceDiagnostic> GetDiagnosticsAt(int offset) =>
    _snapshot.GetGroups(offset, 0).Where(s => offset < s.EndOffset)
      .SelectMany(s => s.Diagnostics).Where(IsVisible).OrderBy(d => d.IsWarning).ToArray();

  internal IReadOnlyList<SourceDiagnostic> GetDiagnosticsAt(Point point, out Rect anchor)
  {
    anchor = Rect.Empty;
    var viewport = new Rect(_view.RenderSize);
    if (!_view.VisualLinesValid || !viewport.Contains(point) || (!_showErrors && !_showWarnings))
      return Array.Empty<SourceDiagnostic>();

    // Query only the visual line under the pointer. Glyph rectangles preserve exact
    // hit testing for wrapped text, tabs, folding and the last character of a span.
    var line = _view.VisualLines.FirstOrDefault(l =>
      point.Y + _view.ScrollOffset.Y >= l.VisualTop
      && point.Y + _view.ScrollOffset.Y < l.VisualTop + l.Height);
    if (line == null) return Array.Empty<SourceDiagnostic>();
    int start = line.FirstDocumentLine.Offset;
    int end = line.LastDocumentLine.EndOffset;
    var matches = new List<DiagnosticSnapshot.DiagnosticGroup>();
    foreach (var group in _snapshot.GetGroups(start, end - start))
    {
      if (!_hitRects.TryGetValue(group, out var rects))
      {
        rects = BackgroundGeometryBuilder.GetRectsForSegment(_view, group).ToArray();
        _hitRects.Add(group, rects);
      }
      foreach (var rect in rects)
      {
        if (!rect.Contains(point)) continue;
        var visible = Rect.Intersect(rect, viewport);
        if (anchor.IsEmpty) anchor = visible;
        else anchor.Union(visible);
        matches.Add(group);
        break;
      }
    }
    if (!_lastHitGroups.SequenceEqual(matches))
    {
      _lastHitGroups = matches;
      _lastHitDiagnostics = matches.SelectMany(g => g.Diagnostics).Where(IsVisible)
        .OrderBy(d => d.IsWarning).ToArray();
    }
    return _lastHitDiagnostics;
  }

  public void Draw(TextView textView, DrawingContext drawingContext)
  {
    if (!textView.VisualLinesValid || textView.VisualLines.Count == 0) return;
    if (!_geometryValid)
    {
      int start = textView.VisualLines[0].FirstDocumentLine.Offset;
      int end = textView.VisualLines[^1].LastDocumentLine.EndOffset;
      _warningGeometry = _showWarnings ? BuildGeometry(start, end, true) : null;
      _errorGeometry = _showErrors ? BuildGeometry(start, end, false) : null;
      _geometryValid = true;
    }
    // Two frozen batches; errors cover warnings at intersections.
    if (_warningGeometry != null) drawingContext.DrawGeometry(null, WarningPen, _warningGeometry);
    if (_errorGeometry != null) drawingContext.DrawGeometry(null, ErrorPen, _errorGeometry);
  }

  private StreamGeometry BuildGeometry(int start, int end, bool warnings)
  {
    var geometry = new StreamGeometry();
    using (var context = geometry.Open())
    {
      foreach (var segment in _snapshot.GetUnderlines(start, end - start, warnings))
      {
        foreach (var rect in BackgroundGeometryBuilder.GetRectsForSegment(_view, segment))
        {
          double left = Math.Max(0, rect.Left);
          double right = Math.Min(_view.ActualWidth, rect.Right);
          double bottom = rect.Bottom - 1;
          if (right <= left || bottom < 0 || bottom > _view.ActualHeight + 2) continue;
          context.BeginFigure(new Point(left, bottom), false, false);
          bool up = true;
          for (double x = left + 2; x < right; x += 2)
          {
            context.LineTo(new Point(x, bottom - (up ? 2 : 0)), true, false);
            up = !up;
          }
          context.LineTo(new Point(right, bottom), true, false);
        }
      }
    }
    geometry.Freeze();
    return geometry;
  }

  private static Pen CreatePen(Color color)
  {
    var pen = new Pen(new SolidColorBrush(color), 1.2);
    pen.Freeze();
    return pen;
  }
}
