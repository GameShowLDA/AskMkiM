using Ask.Core.Services.EventCore.Adapters;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Rendering;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

public class ExecutionGlyphMargin : AbstractMargin
{
  public List<int> ActiveLines { get; } = new();
  public Brush MarkerBrush { get; set; } = Brushes.LimeGreen;

  /// <summary>
  /// Лист поставленных точек остановки
  /// </summary>
  public List<int> BreakpointLines { get; } = new();
  /// <summary>
  /// Цвет точек остановки.
  /// </summary>
  public Brush BreakpointBrush { get; set; } = Brushes.Red;
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

  protected override Size MeasureOverride(Size availableSize)
  {
    return new Size(20, 0);
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

    if (BreakpointLines.Count != 0)
    {
      RenderMargin(BreakpointLines, TextView, verticalOffset, lineHeight, drawingContext, BreakpointBrush);
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
      double top = textView.GetVisualTopByDocumentLine(lineNumber);
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
      oldTextView.ScrollOffsetChanged -= TextView_ScrollOffsetChanged;

    if (newTextView != null)
      newTextView.ScrollOffsetChanged += TextView_ScrollOffsetChanged;
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

    if (BreakpointLines.Contains(lineNumber))
    {
      BreakpointLines.Remove(lineNumber);
      BreakpointEventAdapter.RaiseBreakpointRemoved(lineNumber);
    }
    else
    {
      BreakpointLines.Add(lineNumber);
      BreakpointEventAdapter.RaiseBreakpointSet(lineNumber);
    }

    InvalidateVisual();
    e.Handled = true;
  }

  private void TextView_ScrollOffsetChanged(object? sender, EventArgs e)
  {
    InvalidateVisual();
  }

}