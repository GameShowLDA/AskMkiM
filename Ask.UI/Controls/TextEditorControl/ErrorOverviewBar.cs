using Ask.Core.Services.Errors.Models;
using ICSharpCode.AvalonEdit;
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
  public sealed partial class ErrorOverviewBar : UserControl
  {
    private readonly List<OverviewMarker> _markers = new();
    private IReadOnlyList<OverviewDiagnostic> _diagnostics = Array.Empty<OverviewDiagnostic>();
    private TextEditor? _editor;
    private int _lineCount;
    private Action<int>? _lineClickAction;
    private double _viewportTop;
    private double _viewportBottom = 1.0;
    private int _activeLine = -1;
    private OverviewMarker? _hoveredMarker;
    private IReadOnlyDictionary<int, double>? _linePositions;
    private Action<double>? _positionClickAction;
    private Func<double, string?>? _positionPreviewFactory;
    private double _lastPreviewFraction = -1;

    public ErrorOverviewBar()
    {
      InitializeComponent();
      OverviewSurface.Owner = this;
      SnapsToDevicePixels = true;
      UseLayoutRounding = true;
      Unloaded += (_, _) => _previewPopup.IsOpen = false;
      IsVisibleChanged += (_, _) => { if (!IsVisible) _previewPopup.IsOpen = false; };
      OverviewSurface.SizeChanged += (_, _) => RebuildMarkers();
      MouseMove += ErrorOverviewBar_MouseMove;
      MouseLeave += (_, _) => { _hoveredMarker = null; _lastPreviewFraction = -1; _previewPopup.IsOpen = false; ResetToolTip(); OverviewSurface?.InvalidateVisual(); };
      MouseLeftButtonUp += ErrorOverviewBar_MouseLeftButtonUp;
    }

    public void SetActiveLine(int lineNumber)
    {
      _activeLine = lineNumber;
      OverviewSurface?.InvalidateVisual();
    }

    public void SetEditor(TextEditor editor)
    {
      if (ReferenceEquals(_editor, editor))
        return;

      DetachEditor();
      _editor = editor ?? throw new ArgumentNullException(nameof(editor));
      _lineCount = 0;
      _lineClickAction = null;
      _linePositions = null;
      _positionClickAction = null;
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
      OverviewSurface?.InvalidateVisual();
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

    internal void SetLinePositions(IReadOnlyDictionary<int, double> positions, Action<double> positionClickAction)
    {
      _linePositions = positions;
      _positionClickAction = positionClickAction;
      double maxTop = Math.Max(0, PlotHeight - MarkerHeight);
      foreach (var marker in _markers)
      {
        if (_linePositions.TryGetValue(marker.LineNumber, out double position))
          marker.Top = Math.Clamp(position, 0, 1) * maxTop;
      }
      OverviewSurface?.InvalidateVisual();
    }

    internal void SetPositionPreviewFactory(Func<double, string?>? previewFactory)
    {
      _positionPreviewFactory = previewFactory;
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

    protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
      base.OnPropertyChanged(e);
      if (e.Property == BackgroundProperty)
        OverviewSurface?.InvalidateVisual();
    }

    internal void RenderOverview(DrawingContext drawingContext)
    {

      if (PlotWidth <= 0 || PlotHeight <= 0)
        return;

      var trackBrush = Background ?? TryFindResource("ScrollBarTrackBackground") as Brush
        ?? new SolidColorBrush(Color.FromArgb(28, 128, 128, 128));
      drawingContext.DrawRoundedRectangle(trackBrush, null, new Rect(0, 0, PlotWidth, PlotHeight), TrackCornerRadius, TrackCornerRadius);

      var viewportBorderBrush = ViewportBorderBrush
        ?? TryFindResource("TextEditorLineNumberBrush") as Brush ?? Brushes.SlateGray;
      if (IsViewportVisible)
      {
        var viewportBrush = ViewportBrush ?? viewportBorderBrush;
        double viewportTop = _viewportTop * PlotHeight;
        double viewportBottom = _viewportBottom * PlotHeight;
        drawingContext.PushOpacity(ViewportOpacity);
        drawingContext.DrawRoundedRectangle(viewportBrush, null,
          new Rect(0, viewportTop, PlotWidth, Math.Max(0, viewportBottom - viewportTop)),
          ViewportCornerRadius, ViewportCornerRadius);
        drawingContext.Pop();
        drawingContext.DrawRoundedRectangle(null, new Pen(viewportBorderBrush, ViewportBorderThickness),
          new Rect(0, viewportTop, PlotWidth, Math.Max(0, viewportBottom - viewportTop)),
          ViewportCornerRadius, ViewportCornerRadius);
      }

      foreach (var marker in _markers)
      {
        var brush = GetMarkerBrush(marker.Severity);
        bool active = marker.Lines.Contains(_activeLine);
        bool hovered = ReferenceEquals(marker, _hoveredMarker);
        brush = active ? ActiveMarkerBrush ?? brush : hovered ? HoverMarkerBrush ?? brush : brush;
        var borderBrush = active ? ActiveMarkerBorderBrush ?? viewportBorderBrush
          : hovered ? HoverMarkerBorderBrush ?? MarkerBorderBrush : MarkerBorderBrush;
        double borderWidth = active ? ActiveMarkerBorderThickness
          : hovered ? HoverMarkerBorderThickness : MarkerBorderThickness;
        drawingContext.DrawRoundedRectangle(brush,
          borderBrush == null ? null : new Pen(borderBrush, borderWidth),
          new Rect(MarkerInset, marker.Top, Math.Max(0, PlotWidth - 2 * MarkerInset),
            Math.Min(Math.Max(0, PlotHeight - marker.Top),
              active ? ActiveMarkerHeight : hovered ? HoverMarkerHeight : MarkerHeight)),
          MarkerCornerRadius, MarkerCornerRadius);
      }
    }

    private void RebuildMarkers()
    {
      _markers.Clear();
      _hoveredMarker = null;
      if (_previewPopup != null) _previewPopup.IsOpen = false;

      var document = _editor?.Document;
      int lineCount = document?.LineCount ?? _lineCount;
      if (lineCount <= 0)
      {
        OverviewSurface?.InvalidateVisual();
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
        .Where(marker => IsSeverityVisible(marker.Severity))
        .OrderBy(marker => GetDesiredTop(marker.LineNumber, lineCount, 1))
        .ToList();

      if (ordered.Count == 0)
      {
        OverviewSurface?.InvalidateVisual();
        return;
      }

      double maxTop = Math.Max(0, PlotHeight - MarkerHeight);
      if (IsMarkerGroupingEnabled)
        ordered = CompactNearbyMarkers(ordered, lineCount, maxTop);
      double minimumSpacing = ordered.Count <= 1
        ? 0
        : Math.Min(MarkerHeight + MarkerGap, maxTop / (ordered.Count - 1));

      for (int i = 0; i < ordered.Count; i++)
      {
        var marker = ordered[i];
        double desiredTop = GetDesiredTop(marker.LineNumber, lineCount, maxTop);

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
      OverviewSurface?.InvalidateVisual();
    }

    private List<OverviewMarker> CompactNearbyMarkers(
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

    private double GetDesiredTop(int lineNumber, int lineCount, double maxTop)
    {
      if (_linePositions != null && _linePositions.TryGetValue(lineNumber, out double position))
        return Math.Clamp(position, 0, 1) * maxTop;
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

    private void AddMarker(
      IDictionary<int, OverviewMarker> markersByLine,
      int lineNumber,
      ErrorOverviewSeverity severity,
      string message)
    {
      if (!IsSeverityVisible(severity)) return;
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
      if (severity == ErrorOverviewSeverity.Information) return CommandBrush;
      Brush? configured = severity == ErrorOverviewSeverity.Warning ? WarningBrush : ErrorBrush;
      if (configured != null) return configured;
      string resourceKey = severity switch
      {
        ErrorOverviewSeverity.Warning => "ErrorListWarningIconBrush",
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

    internal bool IsMarkerAt(Point point) =>
      point.X >= MarkerInset && point.X <= PlotWidth - MarkerInset && HitTestMarker(point) != null;

    private OverviewMarker? HitTestMarker(Point point)
    {
      return _markers.FirstOrDefault(marker =>
        point.Y >= marker.Top - MarkerHitPadding
        && point.Y <= marker.Top + (marker.Lines.Contains(_activeLine) ? ActiveMarkerHeight
          : ReferenceEquals(marker, _hoveredMarker) ? HoverMarkerHeight : MarkerHeight) + MarkerHitPadding);
    }

    private void ErrorOverviewBar_MouseMove(object sender, MouseEventArgs e)
    {
      var marker = HitTestMarker(e.GetPosition(OverviewSurface));
      if (!ReferenceEquals(_hoveredMarker, marker))
      {
        _hoveredMarker = marker;
        OverviewSurface?.InvalidateVisual();
      }

      Cursor = IsNavigationEnabled && (marker != null || IsTrackNavigationEnabled) ? Cursors.Hand : Cursors.Arrow;
      if (!AreToolTipsEnabled)
      {
        _previewPopup.IsOpen = false;
        return;
      }
      Point pointer = e.GetPosition(OverviewSurface);
      double fraction = Math.Clamp(pointer.Y / Math.Max(1, PlotHeight), 0, 1);
      string? preview = IsPositionPreviewEnabled ? _positionPreviewFactory?.Invoke(fraction) : null;
      if (marker != null)
      {
        preview = string.IsNullOrWhiteSpace(preview)
          ? marker.ToolTip
          : marker.ToolTip + Environment.NewLine + Environment.NewLine + preview;
      }

      if (!string.IsNullOrWhiteSpace(preview))
      {
        if (Math.Abs(fraction - _lastPreviewFraction) > 0.01 || !_previewPopup.IsOpen)
          _lastPreviewFraction = fraction;
        _previewContent.Content = preview;
        // RelativePoint задаёт координату относительно верхнего левого угла
        // полосы. Поэтому карточка появляется на той же высоте, где находится
        // курсор, и сразу слева от полосы.
        _previewPopup.HorizontalOffset = -PreviewWidth - PreviewHorizontalGap;
        _previewPopup.VerticalOffset = Math.Clamp(pointer.Y + PreviewVerticalOffset, 0, Math.Max(0, PlotHeight - 44));
        _previewPopup.IsOpen = true;
      }
      else
      {
        _lastPreviewFraction = -1;
        _previewPopup.IsOpen = false;
      }
    }

    private void ErrorOverviewBar_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
      if (!IsNavigationEnabled) return;
      NavigateAt(e.GetPosition(OverviewSurface).Y, (Keyboard.Modifiers & ModifierKeys.Shift) != 0);
      e.Handled = true;
    }

    internal void NavigateAt(double y, bool previous)
    {
      if (!IsNavigationEnabled) return;
      var marker = HitTestMarker(new Point(0, y));
      int lineCount = _editor?.Document?.LineCount ?? _lineCount;
      if (lineCount <= 0) return;
      int lineNumber;
      if (marker == null)
      {
        if (!IsTrackNavigationEnabled) return;
        if (_positionClickAction != null)
        {
          _positionClickAction(Math.Clamp(y / Math.Max(1, PlotHeight), 0, 1));
          return;
        }
        lineNumber = 1 + (int)Math.Round(Math.Clamp(y /
          Math.Max(1, PlotHeight - MarkerHeight), 0, 1) * (lineCount - 1));
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
      Cursor = Cursors.Arrow;
    }

    private double PlotWidth => OverviewSurface?.ActualWidth ?? 0;
    private double PlotHeight => OverviewSurface?.ActualHeight ?? 0;

    private bool IsSeverityVisible(ErrorOverviewSeverity severity) => severity switch
    {
      ErrorOverviewSeverity.Information => AreCommandMarkersVisible,
      ErrorOverviewSeverity.Warning => AreWarningMarkersVisible,
      _ => AreErrorMarkersVisible,
    };

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
