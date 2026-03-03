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
  private const double MarkerCenterX = 12;
  private const double ActiveArrowSize = 16;
  private const double ActiveArrowStrokeThickness = 1.4;
  private static readonly Geometry ActiveArrowGeometry = CreateActiveArrowGeometry();

  /// <summary>
  /// Список активных строк выполнения.
  /// </summary>
  public List<int> ActiveLines { get; } = new();

  /// <summary>
  /// Кисть маркера активной строки выполнения.
  /// </summary>
  public Brush MarkerBrush { get; set; } = (Brush)Application.Current.Resources["YellowColorSolidColorBrush"];

  /// <summary>
  /// Цвет фоновой подсветки строки, на которой установлена точка остановки (для <see cref="TextMarkerService"/>).
  /// </summary>
  public Color LineBrush { get; set; } = ((SolidColorBrush)Application.Current.Resources["RedColorSolidColorBrush"]).Color;

  /// <summary>
  /// Определяет, должны ли клики по марджину изменять состояние точек остановки.
  /// </summary>
  public bool BreakpointsInteractive { get; set; } = true;

  /// <summary>
  /// Определяет, должны ли точки остановки визуально отображаться (кружок и подсветка строки).
  /// </summary>
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

  /// <summary>
  /// Устанавливает набор разрешённых строк для клика по марджину.
  /// </summary>
  /// <param name="lines">Список номеров строк, на которые разрешена установка точек.</param>
  public void SetRightBreakpoints(List<int> lines)
  {
    _rightBreakpoints.Clear();
    for (int i = 0; i < lines.Count; i++)
      _rightBreakpoints.Add(lines[i]);
  }

  /// <summary>
  /// Проверяет, установлена ли точка остановки на указанной команде.
  /// </summary>
  /// <param name="commandNumber">Номер команды.</param>
  /// <returns><see langword="true"/>, если точка остановки установлена; иначе <see langword="false"/>.</returns>
  public bool HasBreakpointCommand(int commandNumber) => _bpByCommand.ContainsKey(commandNumber);

  /// <summary>
  /// Гарантирует наличие или отсутствие точки остановки для команды.
  /// Используется для синхронизации состояния между редакторами.
  /// </summary>
  /// <param name="lineNumber">Номер строки, где находится команда.</param>
  /// <param name="commandNumber">Номер команды.</param>
  /// <param name="isSet"><c>true</c> — установить; <c>false</c> — снять.</param>
  /// <param name="raiseEvents">
  /// Нужно ли поднимать события через <see cref="BreakpointEventAdapter"/>.
  /// Для внутренней синхронизации обычно <c>false</c>.
  /// </param>
  public void EnsureBreakpoint(int lineNumber, int commandNumber, bool isSet, bool raiseEvents)
  {
    if (isSet) SetBreakpoint(lineNumber, commandNumber, raiseEvents);
    else RemoveBreakpoint(commandNumber, raiseEvents);
  }

  /// <summary>
  /// Устанавливает точку остановки на указанной строке и команде.
  /// </summary>
  /// <param name="lineNumber">Номер строки в текущем документе.</param>
  /// <param name="commandNumber">Номер команды.</param>
  /// <param name="raiseEvents">Нужно ли поднять событие установки точки остановки.</param>
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

  /// <summary>
  /// Снимает точку остановки для указанной команды.
  /// </summary>
  /// <param name="commandNumber">Номер команды.</param>
  /// <param name="raiseEvents">Нужно ли поднять событие снятия точки остановки.</param>
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

  /// <summary>
  /// Устанавливает активную строку выполнения (очищая предыдущую) и прокручивает редактор к ней.
  /// </summary>
  /// <param name="lineNumber">Номер строки.</param>
  public void SetActiveLine(int lineNumber)
  {
    if (ActiveLines.Count == 1 && ActiveLines[0] == lineNumber) return;

    ActiveLines.Clear();
    ActiveLines.Add(lineNumber);

    InvalidateVisual();
    _textEditor.ScrollTo(lineNumber, 1);
  }

  /// <summary>
  /// Очищает маркеры активной строки выполнения.
  /// </summary>
  public void ClearMarkers()
  {
    if (ActiveLines.Count == 0) return;
    ActiveLines.Clear();
    InvalidateVisual();
  }

  public void ToggleBreakpointFromKeyboard(int lineNumber)
  {
    HandleBreakpointToggle(lineNumber);
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

  protected override Size MeasureOverride(Size availableSize) => new Size(24, 0);

  /// <summary>
  /// Отрисовывает одиночный маркер напротив строки документа.
  /// </summary>
  /// <param name="lineNumber">Номер строки.</param>
  /// <param name="textView">Текущее представление AvalonEdit.</param>
  /// <param name="verticalOffset">Текущая координата по вертикали.</param>
  /// <param name="lineHeight">Высота строки.</param>
  /// <param name="drawingContext">Контекст рисования.</param>
  /// <param name="brush">Кисть для отрисовки.</param>
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
    drawingContext.DrawEllipse(brush, null, new Point(MarkerCenterX, centerY), 8, 8);
  }

  /// <summary>
  /// Builds a filled arrow geometry and rotates it to the right.
  /// </summary>
  private static Geometry CreateActiveArrowGeometry()
  {
    var parsedGeometry = Geometry.Parse(
      "m1.85 11.15 9.8-9.8c.2-.2.5-.2.7 0l9.8 9.8a.5.5 0 0 1-.36.85H16v9a1 1 0 0 1-1 1H9a1 1 0 0 1-1-1v-9H2.2a.5.5 0 0 1-.35-.85Z");

    var geometry = parsedGeometry.Clone();
    geometry.Transform = new RotateTransform(90, 11, 11);
    geometry.Freeze();
    return geometry;
  }

  /// <summary>
  /// Draws active-command marker as a right-pointing arrow centered on the target line.
  /// </summary>
  private static void DrawActiveArrow(DrawingContext drawingContext, Brush brush, double centerY)
  {
    var bounds = ActiveArrowGeometry.Bounds;
    double maxSide = Math.Max(bounds.Width, bounds.Height);
    double scale = maxSide <= 0 ? 1 : ActiveArrowSize / maxSide;
    Brush strokeBrush = (Brush?)Application.Current?.Resources["ForegroundSolidColorBrush"] ?? Brushes.Black;
    var pen = new Pen(strokeBrush, ActiveArrowStrokeThickness);
    if (pen.CanFreeze) pen.Freeze();

    var transform = new TransformGroup();
    transform.Children.Add(new ScaleTransform(scale, scale));
    transform.Children.Add(new TranslateTransform(
      MarkerCenterX - (bounds.X + bounds.Width * 0.5) * scale,
      centerY - (bounds.Y + bounds.Height * 0.5) * scale));

    drawingContext.PushTransform(transform);
    drawingContext.DrawGeometry(brush, pen, ActiveArrowGeometry);
    drawingContext.Pop();
  }

  /// <summary>
  /// Быстро парсит ведущий целочисленный префикс строки (номер команды),
  /// пропуская пробелы и табуляции в начале.
  /// </summary>
  /// <param name="text">Текст строки.</param>
  /// <returns>Распарсенный номер команды.</returns>
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

  /// <summary>
  /// Переключает состояние точки остановки на указанной строке:
  /// если точка есть — снимает, иначе устанавливает.
  /// Номер команды извлекается из фактического текста строки документа.
  /// </summary>
  /// <param name="lineNumber">Номер строки.</param>
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

  /// <summary>
  /// Отрисовывает маркеры напротив набора строк документа.
  /// </summary>
  /// <param name="margin">Список строк.</param>
  /// <param name="textView">Текущее представление AvalonEdit.</param>
  /// <param name="verticalOffset">Текущая координата по вертикали.</param>
  /// <param name="lineHeight">Высота строки.</param>
  /// <param name="drawingContext">Контекст рисования.</param>
  /// <param name="brush">Кисть для отрисовки.</param>
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
      DrawActiveArrow(drawingContext, brush, centerY);
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

    if (!BreakpointsInteractive)
      return;

    TextView.EnsureVisualLines();

    var pos = e.GetPosition(this);
    double visualY = pos.Y + TextView.ScrollOffset.Y;

    int lineNumber = TextView
        .GetDocumentLineByVisualTop(visualY)
        .LineNumber;

    HandleBreakpointToggle(lineNumber);

    e.Handled = true;
  }

  private void HandleBreakpointToggle(int lineNumber)
  {
    if (!_rightBreakpoints.Contains(lineNumber))
      return;

    ToggleBreakpointAtLine(lineNumber);
    InvalidateVisual();
  }

  /// <summary>
  /// Обработчик изменения смещения прокрутки. Перерисовывает марджин.
  /// </summary>
  private void TextView_ScrollOffsetChanged(object? sender, EventArgs e) => InvalidateVisual();

  /// <summary>
  /// Обработчик изменения набора визуальных строк (например, при сворачивании/разворачивании).
  /// Перерисовывает марджин.
  /// </summary>
  private void TextView_VisualLinesChanged(object? sender, EventArgs e) => InvalidateVisual();
}
