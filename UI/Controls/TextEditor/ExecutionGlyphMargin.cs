using Ask.Core.Services.EventCore.Adapters;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Rendering;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using UI.Controls.TextEditor;

public class ExecutionGlyphMargin : AbstractMargin
{
  public List<int> ActiveLines { get; } = new();

  public Brush MarkerBrush { get; set; } = (Brush)Application.Current.Resources["GreenColorSolidColorBrush"];

  public Color LineBrush { get; set; } = ((SolidColorBrush)Application.Current.Resources["RedColorSolidColorBrush"]).Color;

  public bool BreakpointsInteractive { get; set; } = true;
  public bool BreakpointsVisible { get; set; } = true;

  /// <summary>
  /// Лист поставленных точек остановки (для рендера/внешнего доступа).
  /// </summary>
  public List<TextAnchor> BreakpointLines { get; } = new();

  /// <summary>
  /// Лист номеров команд, на которых установлены точки остановки (для внешнего доступа).
  /// </summary>
  public List<int> BreakpointCommandsNumbers { get; } = new();

  /// <summary>
  /// Цвет точек остановки.
  /// </summary>
  public Brush BreakpointBrush { get; set; } = (Brush)Application.Current.Resources["RedColorSolidColorBrush"];

  private readonly HashSet<int> _rightBreakpoints = new();        
  private readonly Dictionary<int, TextAnchor> _bpByCommand = new();

  /// <summary>
  /// Ссылка на редактор AvalonEdit для прокрутки.
  /// </summary>
  private readonly TextEditor _textEditor;

  /// <summary>
  /// Создаёт марджин и связывает его с TextEditor.
  /// </summary>
  /// <param name="textEditor">Экземпляр AvalonEdit.</param>
  public ExecutionGlyphMargin(TextEditor textEditor)
  {
    _textEditor = textEditor;

    _textEditor.DocumentChanged += (_, __) =>
    {
      BreakpointLines.Clear();
      BreakpointCommandsNumbers.Clear();
      _bpByCommand.Clear();
      ActiveLines.Clear();

      RebuildBreakpointLineHighlights();
      InvalidateVisual();
    };
  }

  /// <summary>
  /// Лист, куда можно поставить точки остановки.
  /// </summary>
  public List<int> RightBreakpoints
  {
    get => _rightBreakpoints.ToList();
    set => SetRightBreakpoints(value);
  }

  public void SetRightBreakpoints(List<int> lines)
  {
    _rightBreakpoints.Clear();
    for (int i = 0; i < lines.Count; i++)
      _rightBreakpoints.Add(lines[i]);
  }

  public bool HasBreakpointCommand(int commandNumber) => _bpByCommand.ContainsKey(commandNumber);

  public void EnsureBreakpoint(int lineNumber, int commandNumber, bool isSet, bool raiseEvents)
  {
    if (isSet) SetBreakpoint(lineNumber, commandNumber, raiseEvents);
    else RemoveBreakpoint(commandNumber, raiseEvents);
  }

  private void SetBreakpoint(int lineNumber, int commandNumber, bool raiseEvents)
  {
    if (_bpByCommand.ContainsKey(commandNumber)) return;

    var doc = _textEditor.Document;
    var line = doc.GetLineByNumber(lineNumber);

    var anchor = doc.CreateAnchor(line.Offset);
    anchor.MovementType = AnchorMovementType.BeforeInsertion;
    anchor.SurviveDeletion = false;

    _bpByCommand.Add(commandNumber, anchor);
    BreakpointLines.Add(anchor);
    BreakpointCommandsNumbers.Add(commandNumber);

    anchor.Deleted += (_, __) =>
    {
      _bpByCommand.Remove(commandNumber);
      int idx = BreakpointCommandsNumbers.IndexOf(commandNumber);
      if (idx >= 0)
      {
        BreakpointCommandsNumbers.RemoveAt(idx);
        BreakpointLines.RemoveAt(idx);
      }
      RebuildBreakpointLineHighlights();
      InvalidateVisual();
    };

    RebuildBreakpointLineHighlights();
    InvalidateVisual();

    if (raiseEvents)
      BreakpointEventAdapter.RaiseBreakpointSet(lineNumber, commandNumber);
  }

  private void RemoveBreakpoint(int commandNumber, bool raiseEvents)
  {
    if (!_bpByCommand.TryGetValue(commandNumber, out var anchor)) return;

    _bpByCommand.Remove(commandNumber);

    int idx = BreakpointCommandsNumbers.IndexOf(commandNumber);
    BreakpointCommandsNumbers.RemoveAt(idx);
    BreakpointLines.RemoveAt(idx);

    RebuildBreakpointLineHighlights();
    InvalidateVisual();

    if (raiseEvents)
    {
      int lineNumber = _textEditor.Document.GetLineByOffset(anchor.Offset).LineNumber;
      BreakpointEventAdapter.RaiseBreakpointRemoved(lineNumber, commandNumber);
    }
  }

  public void SetActiveLine(int lineNumber)
  {
    if (ActiveLines.Count == 1 && ActiveLines[0] == lineNumber) return;

    ActiveLines.Clear();
    ActiveLines.Add(lineNumber);

    InvalidateVisual();
    _textEditor.ScrollTo(lineNumber, 1);
  }

  public void ClearMarkers()
  {
    if (ActiveLines.Count == 0) return;
    ActiveLines.Clear();
    InvalidateVisual();
  }

  /// <summary>
  /// Пытается получить экземпляр <see cref="TextMarkerService"/>, зарегистрированный в сервисах визуального слоя AvalonEdit.
  /// </summary>
  /// <remarks>
  /// Сервис маркеров обычно добавляется один раз при инициализации редактора и хранится внутри
  /// <see cref="ICSharpCode.AvalonEdit.Rendering.TextView.Services"/>.
  /// </remarks>
  /// <returns>
  /// Экземпляр <see cref="TextMarkerService"/>, если он доступен; иначе <c>null</c>.
  /// </returns>
  private TextMarkerService? GetMarkerService()
  {
    return _textEditor?.TextArea?.TextView?.Services?
      .GetService(typeof(TextMarkerService)) as TextMarkerService;
  }

  /// <summary>
  /// Пересоздаёт подсветку строк, на которых установлены точки остановки.
  /// </summary>
  private void RebuildBreakpointLineHighlights()
  {
    if (!BreakpointsVisible) return;

    var doc = _textEditor.Document;
    var svc = GetMarkerService();

    svc.ClearAllMarkers();

    for (int i = 0; i < BreakpointLines.Count; i++)
    {
      var line = doc.GetLineByOffset(BreakpointLines[i].Offset);
      svc.AddMarker(line.Offset, line.Length, LineBrush);
    }
  }

  protected override Size MeasureOverride(Size availableSize) => new Size(20, 0);

  private static void RenderMarginSingle(
    int lineNumber,
    TextView textView,
    double verticalOffset,
    double lineHeight,
    DrawingContext drawingContext,
    Brush brush)
  {
    double top = textView.GetVisualTopByDocumentLine(lineNumber);
    if (double.IsNaN(top)) return;

    double centerY = top - verticalOffset + lineHeight * 0.5;
    drawingContext.DrawEllipse(brush, null, new Point(10, centerY), 8, 8);
  }

  private static int ParseLeadingInt(ReadOnlySpan<char> text)
  {
    int i = 0;
    while (text[i] == ' ' || text[i] == '\t') i++;

    int value = 0;
    for (; ; i++)
    {
      int d = text[i] - '0';
      if ((uint)d > 9) break;
      value = value * 10 + d;
    }
    return value;
  }

  private void ToggleBreakpointAtLine(int lineNumber)
  {
    var doc = _textEditor.Document;
    var line = doc.GetLineByNumber(lineNumber);

    int commandNumber = ParseLeadingInt(doc.GetText(line).AsSpan());

    if (_bpByCommand.ContainsKey(commandNumber))
    {
      RemoveBreakpoint(commandNumber, raiseEvents: true);
      return;
    }

    SetBreakpoint(lineNumber, commandNumber, raiseEvents: true);
  }

  protected override void OnRender(DrawingContext drawingContext)
  {
    base.OnRender(drawingContext);

    drawingContext.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, ActualWidth, ActualHeight));

    if (TextView == null || !TextView.VisualLinesValid) return;

    TextView.EnsureVisualLines();

    double verticalOffset = TextView.ScrollOffset.Y;
    double lineHeight = TextView.DefaultLineHeight;

    if (BreakpointsVisible && BreakpointLines.Count != 0)
    {
      var doc = _textEditor.Document;
      for (int i = 0; i < BreakpointLines.Count; i++)
      {
        int lineNumber = doc.GetLineByOffset(BreakpointLines[i].Offset).LineNumber;
        RenderMarginSingle(lineNumber, TextView, verticalOffset, lineHeight, drawingContext, BreakpointBrush);
      }
    }

    if (ActiveLines.Count != 0)
      RenderMargin(ActiveLines, TextView, verticalOffset, lineHeight, drawingContext, MarkerBrush);
  }

  private static void RenderMargin(
    List<int> margin,
    TextView textView,
    double verticalOffset,
    double lineHeight,
    DrawingContext drawingContext,
    Brush brush)
  {
    for (int i = 0; i < margin.Count; i++)
    {
      double top = textView.GetVisualTopByDocumentLine(margin[i] + 1);
      if (double.IsNaN(top)) continue;

      double centerY = top - verticalOffset + lineHeight * 0.5;
      drawingContext.DrawEllipse(brush, null, new Point(10, centerY), 8, 8);
    }
  }

  protected override void OnTextViewChanged(TextView oldTextView, TextView newTextView)
  {
    base.OnTextViewChanged(oldTextView, newTextView);

    if (oldTextView != null)
    {
      oldTextView.ScrollOffsetChanged -= TextView_ScrollOffsetChanged;
      oldTextView.VisualLinesChanged -= TextView_VisualLinesChanged;
    }

    if (newTextView != null)
    {
      newTextView.ScrollOffsetChanged += TextView_ScrollOffsetChanged;
      newTextView.VisualLinesChanged += TextView_VisualLinesChanged;
    }
  }

  protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
  {
    base.OnMouseLeftButtonDown(e);

    if (!BreakpointsInteractive) return;

    TextView.EnsureVisualLines();

    var pos = e.GetPosition(this);
    double visualY = pos.Y + TextView.ScrollOffset.Y;

    int lineNumber = TextView.GetDocumentLineByVisualTop(visualY).LineNumber;

    if (!_rightBreakpoints.Contains(lineNumber)) return;

    ToggleBreakpointAtLine(lineNumber);

    InvalidateVisual();
    e.Handled = true;
  }

  private void TextView_ScrollOffsetChanged(object? sender, EventArgs e) => InvalidateVisual();
  private void TextView_VisualLinesChanged(object? sender, EventArgs e) => InvalidateVisual();
}