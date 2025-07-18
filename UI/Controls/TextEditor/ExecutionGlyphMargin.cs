using System.Windows;
using System.Windows.Media;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Rendering;
using static Utilities.LoggerUtility;

public class ExecutionGlyphMargin : AbstractMargin
{
  public List<int> ActiveLines { get; } = new();
  public Brush MarkerBrush { get; set; } = Brushes.LimeGreen;

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

    if (TextView == null || !TextView.VisualLinesValid || ActiveLines.Count == 0)
      return;

    TextView.EnsureVisualLines();

    // Получаем текущий вертикальный скролл
    double verticalOffset = TextView.ScrollOffset.Y;

    foreach (int lineNumber in ActiveLines)
    {
      double top = TextView.GetVisualTopByDocumentLine(lineNumber);
      if (double.IsNaN(top)) continue;

      double lineHeight = TextView.DefaultLineHeight;
      double centerY = top - verticalOffset + lineHeight / 2; // Сдвигаем вверх на scroll offset

      double radius = 8;
      drawingContext.DrawEllipse(MarkerBrush, null,
          new Point(10, centerY), radius, radius);
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

  private void TextView_ScrollOffsetChanged(object? sender, EventArgs e)
  {
    InvalidateVisual();
  }

}
