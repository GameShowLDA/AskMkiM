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

  /// <summary>
  /// Лист поставленных точек остановки
  /// </summary>
  public List<TextAnchor> BreakpointLines { get; } = new();

  /// <summary>
  /// Лист номеров команд, на которых установлены точки остановки.
  /// </summary>
  public List<int> BreakpointCommandsNumbers { get; } = new();

  /// <summary>
  /// Цвет точек остановки.
  /// </summary>
  public Brush BreakpointBrush { get; set; } = (Brush)Application.Current.Resources["RedColorSolidColorBrush"];

  /// <summary>
  /// Лист, куда можно поставить точки остановки.
  /// </summary>
  public List<int> RightBreakpoints
  {
    get { return new List<int>(_rightBreakpoints); }
    set
    {
      _rightBreakpoints = value ?? new List<int>();
    }
  }
  private List<int> _rightBreakpoints = new();


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
      ActiveLines.Clear();

      RebuildBreakpointLineHighlights();

      InvalidateVisual();
    };

  }

  public void SetActiveLine(int lineNumber)
  {
    if (!Dispatcher.CheckAccess())
    {
      Dispatcher.Invoke(() => SetActiveLine(lineNumber));
      return;
    }

    if (ActiveLines.Count == 1 && ActiveLines[0] == lineNumber)
      return;

    ActiveLines.Clear();
    ActiveLines.Add(lineNumber);

    InvalidateVisual();

    _textEditor.ScrollTo(lineNumber, 1);
  }

  public void ClearMarkers()
  {
    if (!Dispatcher.CheckAccess())
    {
      Dispatcher.Invoke(ClearMarkers);
      return;
    }

    if (ActiveLines.Count > 0)
    {
      ActiveLines.Clear();
      InvalidateVisual();
    }
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
    var doc = _textEditor.Document;
    var svc = GetMarkerService();
    if (doc == null || svc == null) return;

    svc.ClearAllMarkers();

    DocumentLine line;

    foreach (var anchor in BreakpointLines)
    {
      line = doc.GetLineByOffset(anchor.Offset);
      svc.AddMarker(line.Offset, line.Length, LineBrush);
    }
  }

  protected override Size MeasureOverride(Size availableSize)
  {
    return new Size(20, 0);
  }

  private static void RenderMarginSingle(int lineNumber,
    TextView textView,
    double verticalOffset,
    double lineHeight,
    DrawingContext drawingContext,
    Brush brush)
  {
    double top = textView.GetVisualTopByDocumentLine(lineNumber);
    if (double.IsNaN(top)) return;

    double centerY = top - verticalOffset + lineHeight / 2;
    drawingContext.DrawEllipse(brush, null, new Point(10, centerY), 8, 8);
  }

  public static int GetNumberAtLine(ReadOnlySpan<char> text, int targetLine)
  {
    int line = 1, i = 0;

    if (targetLine != 1)
      for (; ; i++)
      {
        char c = text[i];
        if (c == '\n' || c == '\r')
        {
          if (c == '\r' && text[i + 1] == '\n') i++;
          if (++line == targetLine) { i++; break; }
        }
      }

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

  private void ToggleBreakpointAnchor(int lineNumber)
  {
    var doc = _textEditor.Document;
    if (doc == null) return;

    var line = doc.GetLineByNumber(lineNumber);
    int offset = line.Offset;

    var existing = BreakpointLines.FirstOrDefault(a =>
      doc.GetLineByOffset(a.Offset).LineNumber == lineNumber);

    int commandNumber;

    if (existing != null)
    {
      commandNumber = GetNumberAtLine(existing.Document.Text, existing.Line);
      BreakpointLines.Remove(existing);
      BreakpointCommandsNumbers.Remove(commandNumber);
      BreakpointEventAdapter.RaiseBreakpointRemoved(lineNumber, commandNumber);
      RebuildBreakpointLineHighlights();
      return;
    }

    var anchor = doc.CreateAnchor(offset);
    anchor.MovementType = AnchorMovementType.BeforeInsertion;
    anchor.SurviveDeletion = false;

    anchor.Deleted += (_, __) =>
    {
      BreakpointLines.Remove(anchor);
      BreakpointCommandsNumbers.Remove(GetNumberAtLine(anchor.Document.Text, anchor.Line));
      InvalidateVisual();
      RebuildBreakpointLineHighlights();
    };

    commandNumber = GetNumberAtLine(anchor.Document.Text, anchor.Line);
    BreakpointCommandsNumbers.Add(commandNumber);
    BreakpointLines.Add(anchor);
    RebuildBreakpointLineHighlights();
    BreakpointEventAdapter.RaiseBreakpointSet(lineNumber, commandNumber);
  }

  protected override void OnRender(DrawingContext drawingContext)
  {
    base.OnRender(drawingContext);

    drawingContext.DrawRectangle(
        Brushes.Transparent,
        null,
        new Rect(0, 0, ActualWidth, ActualHeight));

    if (TextView == null || !TextView.VisualLinesValid)
      return;

    TextView.EnsureVisualLines();

    double verticalOffset = TextView.ScrollOffset.Y;
    double lineHeight = TextView.DefaultLineHeight;

    if (_textEditor.Document != null && BreakpointLines.Count != 0)
    {
      foreach (var anchor in BreakpointLines)
      {
        int lineNumber = _textEditor.Document.GetLineByOffset(anchor.Offset).LineNumber;
        RenderMarginSingle(lineNumber, TextView, verticalOffset, lineHeight, drawingContext, BreakpointBrush);
      }
    }

    if (ActiveLines.Count != 0)
    {
      RenderMargin(ActiveLines, TextView, verticalOffset, lineHeight, drawingContext, MarkerBrush);
    }
  }

  /// <summary>
  /// Отрисовка точек в левой области редактора напротив указанных строк документа.
  /// </summary>
  private static void RenderMargin(
    List<int> margin,
    TextView textView,
    double verticalOffset,
    double lineHeight,
    DrawingContext drawingContext,
    Brush brush
    )
  {
    foreach (int lineNumber in margin)
    {
      double top = textView.GetVisualTopByDocumentLine(lineNumber+1);
      if (double.IsNaN(top)) continue;

      double centerY = top - verticalOffset + lineHeight / 2;
      drawingContext.DrawEllipse(
          brush,
          null,
          new Point(10, centerY),
          8, 8);
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

    TextView.EnsureVisualLines();

    var pos = e.GetPosition(this);
    double visualY = pos.Y + TextView.ScrollOffset.Y;

    var docLine = TextView.GetDocumentLineByVisualTop(visualY);

    int lineNumber = docLine.LineNumber;

    if (!_rightBreakpoints.Contains(lineNumber)) return;

    ToggleBreakpointAnchor(lineNumber);

    InvalidateVisual();
    e.Handled = true;
  }

  private void TextView_ScrollOffsetChanged(object? sender, EventArgs e)
  {
    InvalidateVisual();
  }

  private void TextView_VisualLinesChanged(object? sender, EventArgs e)
  {
    InvalidateVisual();
  }

}