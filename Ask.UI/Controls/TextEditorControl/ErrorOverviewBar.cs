using Ask.Core.Services.Errors.Models;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Ask.UI.Controls.TextEditorControl
{
  public enum ErrorOverviewSeverity
  {
    Information,
    Warning,
    Error,
  }

  /// <summary>
  /// Узкая полоса обзора диагностик для WPF-редактора и протокола.
  /// </summary>
  public sealed class ErrorOverviewBar : FrameworkElement
  {
    private const double MarkerHeight = 3.0;
    private const double MarkerGap = 1.0;

    private readonly List<OverviewMarker> _markers = new();
    private IReadOnlyList<OverviewDiagnostic> _diagnostics = Array.Empty<OverviewDiagnostic>();
    private TextEditor? _editor;
    private int _lineCount;
    private Action<int>? _lineClickAction;
    private double _viewportTop;
    private double _viewportBottom = 1.0;
    private int _activeLine = -1;
    private OverviewMarker? _hoveredMarker;

    public ErrorOverviewBar()
    {
      SnapsToDevicePixels = true;
      UseLayoutRounding = true;
      Cursor = Cursors.Arrow;

      SizeChanged += (_, _) => RebuildMarkers();
      MouseMove += ErrorOverviewBar_MouseMove;
      MouseLeave += (_, _) => { _hoveredMarker = null; ResetToolTip(); InvalidateVisual(); };
      MouseLeftButtonUp += ErrorOverviewBar_MouseLeftButtonUp;
    }

    public void SetActiveLine(int lineNumber)
    {
      _activeLine = lineNumber;
      InvalidateVisual();
    }

    public void SetEditor(TextEditor editor)
    {
      if (ReferenceEquals(_editor, editor))
        return;

      DetachEditor();
      _editor = editor ?? throw new ArgumentNullException(nameof(editor));
      _lineCount = 0;
      _lineClickAction = null;
      _editor.TextChanged += Editor_TextChanged;
      _editor.DocumentChanged += Editor_DocumentChanged;
      _editor.TextArea.TextView.ScrollOffsetChanged += TextView_Changed;
      _editor.TextArea.TextView.VisualLinesChanged += TextView_Changed;
      RebuildMarkers();
    }

    public void SetViewport(double topFraction, double bottomFraction)
    {
      double top = Math.Clamp(topFraction, 0.0, 1.0);
      double bottom = Math.Clamp(bottomFraction, 0.0, 1.0);

      if (bottom < top)
        (top, bottom) = (bottom, top);

      if (Math.Abs(_viewportTop - top) < double.Epsilon &&
          Math.Abs(_viewportBottom - bottom) < double.Epsilon)
      {
        return;
      }

      _viewportTop = top;
      _viewportBottom = bottom;
      InvalidateVisual();
    }

    public void SetLineDiagnostics(
      int lineCount,
      IEnumerable<(int LineNumber, ErrorOverviewSeverity Severity, string Message)>? diagnostics,
      Action<int>? lineClickAction = null)
    {
      DetachEditor();
      _lineCount = Math.Max(0, lineCount);
      _lineClickAction = lineClickAction;
      _diagnostics = (diagnostics ?? Array.Empty<(int, ErrorOverviewSeverity, string)>())
        .Select(diagnostic => new OverviewDiagnostic(
          diagnostic.LineNumber,
          diagnostic.Severity,
          diagnostic.Message ?? string.Empty))
        .ToArray();

      RebuildMarkers();
    }

    public void SetIssues(IEnumerable<IDisplayIssue>? issues, bool useFormattedLineNumber = false)
    {
      _diagnostics = (issues ?? Array.Empty<IDisplayIssue>())
        .Select(issue => new OverviewDiagnostic(
          useFormattedLineNumber && issue.FormattedLineNumber > 0
            ? issue.FormattedLineNumber
            : issue.SourceLineNumber,
          issue.IsWarning ? ErrorOverviewSeverity.Warning : ErrorOverviewSeverity.Error,
          BuildToolTip(issue)))
        .ToArray();

      RebuildMarkers();
    }

    public void SetDiagnostics(
      IEnumerable<(int LineNumber, ErrorOverviewSeverity Severity, string Message)>? diagnostics)
    {
      _diagnostics = (diagnostics ?? Array.Empty<(int, ErrorOverviewSeverity, string)>())
        .Select(diagnostic => new OverviewDiagnostic(
          diagnostic.LineNumber,
          diagnostic.Severity,
          diagnostic.Message ?? string.Empty))
        .ToArray();

      RebuildMarkers();
    }

    public void AddIssue(IDisplayIssue issue, bool useFormattedLineNumber = false)
    {
      ArgumentNullException.ThrowIfNull(issue);

      var lineNumber = useFormattedLineNumber && issue.FormattedLineNumber > 0
        ? issue.FormattedLineNumber
        : issue.SourceLineNumber;

      _diagnostics = _diagnostics
        .Append(new OverviewDiagnostic(
          lineNumber,
          issue.IsWarning ? ErrorOverviewSeverity.Warning : ErrorOverviewSeverity.Error,
          BuildToolTip(issue)))
        .ToArray();

      RebuildMarkers();
    }

    public void ClearIssues()
    {
      _diagnostics = Array.Empty<OverviewDiagnostic>();
      RebuildMarkers();
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
      base.OnRender(drawingContext);

      if (ActualWidth <= 0 || ActualHeight <= 0)
        return;

      var trackBrush = TryFindResource("ScrollBarTrackBackground") as Brush
        ?? new SolidColorBrush(Color.FromArgb(28, 128, 128, 128));
      drawingContext.DrawRoundedRectangle(trackBrush, null, new Rect(0, 0, ActualWidth, ActualHeight), 5, 5);

      var viewportBorderBrush = TryFindResource("TextEditorLineNumberBrush") as Brush ?? Brushes.SlateGray;
      var viewportBrush = viewportBorderBrush.Clone();
      viewportBrush.Opacity = 0.14;
      double viewportTop = _viewportTop * ActualHeight;
      double viewportBottom = _viewportBottom * ActualHeight;
      drawingContext.DrawRectangle(
        viewportBrush,
        new Pen(viewportBorderBrush, 1),
        new Rect(0, viewportTop, ActualWidth, Math.Max(1, viewportBottom - viewportTop)));

      foreach (var marker in _markers)
      {
        var brush = GetMarkerBrush(marker.Severity);
        bool active = marker.Lines.Contains(_activeLine);
        drawingContext.DrawRoundedRectangle(
          brush,
          active ? new Pen(viewportBorderBrush, 2) : null,
          new Rect(3, marker.Top, Math.Max(1, ActualWidth - 6),
            active || ReferenceEquals(marker, _hoveredMarker) ? MarkerHeight + 2 : MarkerHeight),
          1,
          1);
      }
    }

    private void RebuildMarkers()
    {
      _markers.Clear();

      var document = _editor?.Document;
      int lineCount = document?.LineCount ?? _lineCount;
      if (lineCount <= 0)
      {
        InvalidateVisual();
        return;
      }

      var markersByLine = new Dictionary<int, OverviewMarker>();

      if (document != null)
      {
        foreach (var line in document.Lines)
        {
          var lineText = document.GetText(line.Offset, line.Length);
          if (lineText.Contains("БРАК", StringComparison.OrdinalIgnoreCase))
          {
            AddMarker(
              markersByLine,
              line.LineNumber,
              ErrorOverviewSeverity.Error,
              lineText.Trim());
          }
        }
      }

      foreach (var diagnostic in _diagnostics)
      {
        int lineNumber = diagnostic.LineNumber;

        if (lineNumber <= 0 || lineNumber > lineCount)
          continue;

        AddMarker(
          markersByLine,
          lineNumber,
          diagnostic.Severity,
          diagnostic.Message);
      }

      var ordered = markersByLine.Values
        .OrderBy(marker => marker.LineNumber)
        .ToList();

      if (ordered.Count == 0)
      {
        InvalidateVisual();
        return;
      }

      double maxTop = Math.Max(0, ActualHeight - MarkerHeight);
      ordered = CompactNearbyMarkers(ordered, lineCount, maxTop);
      double minimumSpacing = ordered.Count <= 1
        ? 0
        : Math.Min(MarkerHeight + MarkerGap, maxTop / (ordered.Count - 1));

      for (int i = 0; i < ordered.Count; i++)
      {
        var marker = ordered[i];
        double desiredTop = lineCount <= 1
          ? 0
          : (marker.LineNumber - 1) * maxTop / (lineCount - 1);

        marker.Top = i == 0
          ? desiredTop
          : Math.Max(desiredTop, ordered[i - 1].Top + minimumSpacing);
      }

      if (ordered[^1].Top > maxTop)
      {
        ordered[^1].Top = maxTop;
        for (int i = ordered.Count - 2; i >= 0; i--)
        {
          ordered[i].Top = Math.Min(
            ordered[i].Top,
            ordered[i + 1].Top - minimumSpacing);
        }
      }

      _markers.AddRange(ordered);
      InvalidateVisual();
    }

    private static List<OverviewMarker> CompactNearbyMarkers(
      IReadOnlyList<OverviewMarker> markers,
      int lineCount,
      double maxTop)
    {
      if (markers.Count <= 1)
        return markers.ToList();

      var compacted = new List<OverviewMarker>();
      compacted.Add(markers[0]);

      for (int i = 1; i < markers.Count; i++)
      {
        var marker = markers[i];
        double previousTop = GetDesiredTop(compacted[^1].LineNumber, lineCount, maxTop);
        double currentTop = GetDesiredTop(marker.LineNumber, lineCount, maxTop);

        if (currentTop - previousTop <= MarkerHeight + MarkerGap)
        {
          MergeMarkers(compacted[^1], marker);
        }
        else
        {
          compacted.Add(marker);
        }

      }

      return compacted;
    }

    private static double GetDesiredTop(int lineNumber, int lineCount, double maxTop)
    {
      return lineCount <= 1
        ? 0
        : (lineNumber - 1) * maxTop / (lineCount - 1);
    }

    private static void MergeMarkers(OverviewMarker target, OverviewMarker source)
    {
      target.Count += source.Count;
      target.Lines.AddRange(source.Lines);
      if (source.Severity > target.Severity)
        target.Severity = source.Severity;

      if (!string.IsNullOrWhiteSpace(source.Message) &&
          !target.Message.Contains(source.Message, StringComparison.Ordinal))
      {
        target.Message = string.IsNullOrWhiteSpace(target.Message)
          ? source.Message
          : target.Message.Length < 600 ? target.Message + Environment.NewLine + source.Message : target.Message;
      }
    }

    private static void AddMarker(
      IDictionary<int, OverviewMarker> markersByLine,
      int lineNumber,
      ErrorOverviewSeverity severity,
      string message)
    {
      if (!markersByLine.TryGetValue(lineNumber, out var marker))
      {
        markersByLine[lineNumber] = new OverviewMarker(lineNumber, severity, message);
        return;
      }

      if (severity > marker.Severity)
        marker.Severity = severity;

      if (!string.IsNullOrWhiteSpace(message) && !marker.Message.Contains(message, StringComparison.Ordinal))
        marker.Message = string.IsNullOrWhiteSpace(marker.Message)
          ? message
          : marker.Message + Environment.NewLine + message;
    }

    private static string BuildToolTip(IDisplayIssue issue)
    {
      var parts = new[]
      {
        issue.Description,
        issue.Command,
        issue.MeasureResult,
      };

      return string.Join(
        Environment.NewLine,
        parts.Where(part => !string.IsNullOrWhiteSpace(part)).Distinct(StringComparer.Ordinal));
    }

    private Brush GetMarkerBrush(ErrorOverviewSeverity severity)
    {
      string resourceKey = severity switch
      {
        ErrorOverviewSeverity.Warning => "ErrorListWarningIconBrush",
        ErrorOverviewSeverity.Information => "TestsProtocolMessageForeground",
        _ => "ErrorListErrorIconBrush",
      };

      if (TryFindResource(resourceKey) is Brush brush)
        return brush;

      return severity switch
      {
        ErrorOverviewSeverity.Warning => Brushes.Orange,
        ErrorOverviewSeverity.Information => Brushes.DodgerBlue,
        _ => Brushes.Red,
      };
    }

    private OverviewMarker? HitTestMarker(Point point)
    {
      return _markers.FirstOrDefault(marker =>
        point.Y >= marker.Top - MarkerGap
        && point.Y <= marker.Top + MarkerHeight + MarkerGap);
    }

    private void ErrorOverviewBar_MouseMove(object sender, MouseEventArgs e)
    {
      var marker = HitTestMarker(e.GetPosition(this));
      if (!ReferenceEquals(_hoveredMarker, marker))
      {
        _hoveredMarker = marker;
        InvalidateVisual();
      }
      if (marker == null)
      {
        ResetToolTip();
        Cursor = Cursors.Arrow;
        return;
      }

      if (ToolTip is not TextBlock text || text.Text != marker.ToolTip)
        ToolTip = new TextBlock { Text = marker.ToolTip, MaxWidth = 360, TextWrapping = TextWrapping.Wrap };
      Cursor = Cursors.Hand;
    }

    private void ErrorOverviewBar_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
      NavigateAt(e.GetPosition(this).Y, (Keyboard.Modifiers & ModifierKeys.Shift) != 0);
      e.Handled = true;
    }

    internal void NavigateAt(double y, bool previous)
    {
      var marker = HitTestMarker(new Point(0, y));
      int lineCount = _editor?.Document?.LineCount ?? _lineCount;
      if (lineCount <= 0) return;
      int lineNumber;
      if (marker == null)
      {
        lineNumber = 1 + (int)Math.Round(Math.Clamp(y /
          Math.Max(1, ActualHeight - MarkerHeight), 0, 1) * (lineCount - 1));
      }
      else
      {
        int current = marker.Lines.IndexOf(_activeLine);
        int next = current < 0 ? (previous ? marker.Lines.Count - 1 : 0)
          : (current + (previous ? -1 : 1) + marker.Lines.Count) % marker.Lines.Count;
        lineNumber = marker.Lines[next];
      }
      SetActiveLine(lineNumber);

      if (_editor != null)
      {
        _editor.ScrollToLine(lineNumber);
        _editor.TextArea.Focus();
      }
      else
      {
        _lineClickAction?.Invoke(lineNumber);
      }

    }

    private void ResetToolTip()
    {
      ToolTip = null;
      Cursor = Cursors.Arrow;
    }

    private void Editor_TextChanged(object? sender, EventArgs e) => RebuildMarkers();

    private void Editor_DocumentChanged(object? sender, EventArgs e) => RebuildMarkers();

    private void TextView_Changed(object? sender, EventArgs e)
    {
      if (_editor?.Document is not { } document) return;
      var view = _editor.TextArea.TextView;
      if (!view.VisualLinesValid || view.VisualLines.Count == 0) return;
      SetViewport((double)(view.VisualLines[0].FirstDocumentLine.LineNumber - 1) / document.LineCount,
        (double)view.VisualLines[^1].LastDocumentLine.LineNumber / document.LineCount);
    }

    private void DetachEditor()
    {
      if (_editor == null)
        return;

      _editor.TextChanged -= Editor_TextChanged;
      _editor.DocumentChanged -= Editor_DocumentChanged;
      _editor.TextArea.TextView.ScrollOffsetChanged -= TextView_Changed;
      _editor.TextArea.TextView.VisualLinesChanged -= TextView_Changed;
      _editor = null;
    }

    private sealed class OverviewMarker
    {
      public OverviewMarker(int lineNumber, ErrorOverviewSeverity severity, string message)
      {
        LineNumber = lineNumber;
        Lines.Add(lineNumber);
        Severity = severity;
        Message = message;
      }

      public int LineNumber { get; }
      public List<int> Lines { get; } = new();
      public ErrorOverviewSeverity Severity { get; set; }
      public string Message { get; set; }
      public int Count { get; set; } = 1;
      public double Top { get; set; }

      public string ToolTip => Count > 1
        ? $"Сообщений: {Count}; первое — строка {LineNumber}{Environment.NewLine}{Message}"
        : $"Строка {LineNumber}{Environment.NewLine}{Message}";
    }

    private sealed record OverviewDiagnostic(
      int LineNumber,
      ErrorOverviewSeverity Severity,
      string Message);
  }
}
