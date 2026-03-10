using Ask.Core.Services.EventCore.Adapters;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Folding;
using ICSharpCode.AvalonEdit.Rendering;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using UI.Controls.TextEditor;

public class ExecutionGlyphMargin : AbstractMargin
{
  private const double MarkerCenterX = 12;
  private const double ActiveArrowSize = 16;
  private const double ActiveArrowStrokeThickness = 1.4;
  private const double ActiveRangeLinePadding = 2;
  private const double ActiveRangeLineThickness = 4;
  private const double ActiveRangeBackgroundLeftOverflow = 80;
  private const byte BreakpointBackgroundAlpha = 72;
  private const string ActiveRangeStripeBrushKey = "TextEditorActiveCommandStripeBrush";
  private const string ActiveRangeBackgroundBrushKey = "TextEditorActiveCommandBackgroundBrush";
  private static readonly Geometry ActiveArrowGeometry = CreateActiveArrowGeometry();
  private static readonly Regex CommandHeaderRegex = new(@"^\s*\d+\s+[А-ЯA-Z]{2,}\b", RegexOptions.Compiled);

  /// <summary>
  /// Список активных строк выполнения.
  /// </summary>
  public List<int> ActiveLines { get; } = new();

  /// <summary>
  /// Кисть маркера активной строки выполнения.
  /// </summary>
  public Brush MarkerBrush { get; set; } = (Brush)Application.Current.Resources["YellowColorSolidColorBrush"];

  /// <summary>
  /// Кисть вертикальной линии диапазона активной команды.
  /// </summary>
  public Brush ActiveRangeBrush { get; set; } =
    (Brush?)Application.Current?.Resources[ActiveRangeStripeBrushKey]
    ?? (Brush?)Application.Current?.Resources["ForegroundSolidColorBrush"]
    ?? Brushes.DodgerBlue;

  /// <summary>
  /// Кисть фоновой заливки диапазона активной команды.
  /// </summary>
  public Brush ActiveRangeBackgroundBrush { get; set; } =
    (Brush?)Application.Current?.Resources[ActiveRangeBackgroundBrushKey]
    ?? (Brush?)Application.Current?.Resources["TestsProtocolBackgroundBrush"]
    ?? Brushes.Transparent;

  /// <summary>
  /// Базовый цвет подсветки точки останова.
  /// </summary>
  public Color LineBrush { get; set; } = ((SolidColorBrush)Application.Current.Resources["RedColorSolidColorBrush"]).Color;

  /// <summary>
  /// Цвет накладываемого кружка для отключённой точки остановки.
  /// </summary>
  private static readonly Brush BreakpointHoleBrush =
    (Brush)new BrushConverter().ConvertFromString("#303843");

  /// <summary>
  /// Кисть фоновой заливки диапазона команды с точкой останова.
  /// </summary>
  public Brush BreakpointRangeBackgroundBrush { get; private set; } = Brushes.Transparent;

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
  private readonly HashSet<int> _disabledBreakpoints = new();
  private readonly ActiveCommandRangeBackgroundRenderer _activeRangeBackgroundRenderer;
  private readonly BreakpointRangeBackgroundRenderer _breakpointRangeBackgroundRenderer;
  private int _activeRangeStartLine = -1; // 0-based line number
  private int _activeRangeEndLine = -1;   // 0-based line number

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
    RefreshThemeBrushes();
    _activeRangeBackgroundRenderer = new ActiveCommandRangeBackgroundRenderer(this);
    _breakpointRangeBackgroundRenderer = new BreakpointRangeBackgroundRenderer(this);
    _textEditor.TextArea.TextView.BackgroundRenderers.Add(_activeRangeBackgroundRenderer);
    _textEditor.TextArea.TextView.BackgroundRenderers.Add(_breakpointRangeBackgroundRenderer);

    _textEditor.DocumentChanged += (_, __) =>
    {
      BreakpointLines.Clear();
      BreakpointCommandsNumbers.Clear();
      _bpByCommand.Clear();
      _disabledBreakpoints.Clear();
      ActiveLines.Clear();
      _activeRangeStartLine = -1;
      _activeRangeEndLine = -1;

      RebuildBreakpointLineHighlights();
      InvalidateVisual();
      InvalidateEditorBackground();
    };
  }

  /// <summary>
  /// Подтягивает кисти активной команды из текущей темы.
  /// </summary>
  private void RefreshThemeBrushes()
  {
    ActiveRangeBrush =
      (Brush?)Application.Current?.Resources[ActiveRangeStripeBrushKey]
      ?? (Brush?)Application.Current?.Resources["ForegroundSolidColorBrush"]
      ?? ActiveRangeBrush;

    ActiveRangeBackgroundBrush =
      (Brush?)Application.Current?.Resources[ActiveRangeBackgroundBrushKey]
      ?? (Brush?)Application.Current?.Resources["TestsProtocolBackgroundBrush"]
      ?? ActiveRangeBackgroundBrush;

    if (Application.Current?.Resources["RedColorSolidColorBrush"] is SolidColorBrush redBrush)
    {
      LineBrush = redBrush.Color;
      BreakpointBrush = redBrush;
      BreakpointRangeBackgroundBrush = CreateTransparentBrush(redBrush.Color, BreakpointBackgroundAlpha);
    }
  }

  private static Brush CreateTransparentBrush(Color baseColor, byte alpha)
  {
    var brush = new SolidColorBrush(Color.FromArgb(alpha, baseColor.R, baseColor.G, baseColor.B));
    if (brush.CanFreeze)
      brush.Freeze();

    return brush;
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
  /// Создаёт марджин номеров строк с заливкой активного диапазона команды.
  /// </summary>
  public LineNumberMargin CreateRangeAwareLineNumberMargin()
  {
    return new ActiveRangeLineNumberMargin(this);
  }

  /// <summary>
  /// Создаёт марджин сворачивания с заливкой активного диапазона команды.
  /// </summary>
  public AbstractMargin CreateRangeAwareFoldingMargin()
  {
    return new ActiveRangeFoldingMargin(this);
  }

  /// <summary>
  /// Устанавливает набор разрешённых строк для клика по марджину.
  /// </summary>
  /// <param name="lines">Список номеров строк, на которые разрешена установка точек.</param>
  public void SetRightBreakpoints(List<int> lines)
  {
    _rightBreakpoints.Clear();
    if (lines == null) return;

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
    else RemoveBreakpoint(lineNumber, commandNumber, raiseEvents);
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
    _disabledBreakpoints.Remove(commandNumber);

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
  /// Снимает точку остановки для указанной команды, привязанной к указанной строке.
  /// </summary>
  /// <param name="lineNumber">Номер строки в текущем документе.</param>
  /// <param name="commandNumber">Номер команды.</param>
  /// <param name="raiseEvents">Нужно ли поднять событие снятия точки остановки.</param>
  private void RemoveBreakpoint(int lineNumber, int commandNumber, bool raiseEvents)
  {
    if (!_bpByCommand.TryGetValue(commandNumber, out var anchor)) return;

    _bpByCommand.Remove(commandNumber);
    _disabledBreakpoints.Remove(commandNumber);

    int idx = BreakpointCommandsNumbers.IndexOf(commandNumber);
    if (idx >= 0)
    {
      BreakpointCommandsNumbers.RemoveAt(idx);
      BreakpointLines.RemoveAt(idx);
    }

    RebuildBreakpointLineHighlights();
    InvalidateVisual();

    if (raiseEvents)
      BreakpointEventAdapter.RaiseBreakpointRemoved(lineNumber, commandNumber);
  }

  /// <summary>
  /// Проверяет, включена ли точка остановки на команде.
  /// </summary>
  /// <remarks>Если точки нет — вернёт <see langword="false"/>.</remarks>
  public bool IsBreakpointEnabled(int commandNumber)
  {
    return _bpByCommand.ContainsKey(commandNumber) && !_disabledBreakpoints.Contains(commandNumber);
  }

  /// <summary>
  /// Включает точку остановки для команды.
  /// </summary>
  public void EnableBreakpoint(int commandNumber, bool raiseEvents)
  {
    if (!_bpByCommand.TryGetValue(commandNumber, out var anchor))
      return;

    _disabledBreakpoints.Remove(commandNumber);

    RebuildBreakpointLineHighlights();
    InvalidateVisual();

    if (!raiseEvents) return;

    int line0 = _textEditor.Document.GetLineByOffset(anchor.Offset).LineNumber;
    BreakpointEventAdapter.RaiseBreakpointOn(line0, commandNumber);
  }

  /// <summary>
  /// Выключает точку остановки для команды (точка должна существовать).
  /// </summary>
  public void DisableBreakpoint(int commandNumber, bool raiseEvents)
  {
    if (!_bpByCommand.TryGetValue(commandNumber, out var anchor))
      return;

    _disabledBreakpoints.Add(commandNumber);

    RebuildBreakpointLineHighlights();
    InvalidateVisual();

    if (!raiseEvents) return;

    int line0 = _textEditor.Document.GetLineByOffset(anchor.Offset).LineNumber;
    BreakpointEventAdapter.RaiseBreakpointOff(line0, commandNumber);
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
    UpdateActiveRangeFromStart(lineNumber);

    InvalidateVisual();
    InvalidateEditorBackground();
    _textEditor.ScrollTo(lineNumber, 1);
  }

  /// <summary>
  /// Очищает маркеры активной строки выполнения.
  /// </summary>
  public void ClearMarkers()
  {
    if (ActiveLines.Count == 0) return;
    ActiveLines.Clear();
    _activeRangeStartLine = -1;
    _activeRangeEndLine = -1;
    InvalidateVisual();
    InvalidateEditorBackground();
  }

  /// <summary>
  /// Возвращает активный диапазон команд в номерах строк документа (1-based).
  /// </summary>
  public bool TryGetActiveRange(out int startDocLine, out int endDocLine)
  {
    if (_activeRangeStartLine < 0 || _activeRangeEndLine < _activeRangeStartLine)
    {
      startDocLine = -1;
      endDocLine = -1;
      return false;
    }

    startDocLine = _activeRangeStartLine + 1;
    endDocLine = _activeRangeEndLine + 1;
    return true;
  }

  public void ToggleBreakpointFromKeyboard(int lineNumber)
  {
    HandleBreakpointToggle(lineNumber);
  }

  /// <summary>
  /// Вычисляет диапазон команды по строкам документа (1-based):
  /// от заголовка команды до строки перед следующим заголовком.
  /// </summary>
  private bool TryGetCommandLineRange(int startDocLine, out int endDocLine)
  {
    var document = _textEditor.Document;
    endDocLine = -1;

    if (document == null || document.LineCount == 0)
      return false;

    int startLine = Math.Clamp(startDocLine, 1, document.LineCount);
    int endLine = document.LineCount;

    for (int lineNumber = startLine + 1; lineNumber <= document.LineCount; lineNumber++)
    {
      var line = document.GetLineByNumber(lineNumber);
      var text = document.GetText(line);

      if (!CommandHeaderRegex.IsMatch(text))
        continue;

      endLine = lineNumber - 1;
      break;
    }

    endDocLine = Math.Max(startLine, endLine);
    return true;
  }

  /// <summary>
  /// Инвалидирует фон для перерисовки диапазонов команд с точками останова.
  /// </summary>
  private void RebuildBreakpointLineHighlights()
  {
    InvalidateEditorBackground();
  }

  protected override Size MeasureOverride(Size availableSize) => new Size(24, 0);

  /// <summary>
  /// Отрисовывает одиночный маркер напротив строки документа.
  /// </summary>
  /// <remarks>Дополнительно может отрисовать поверх ещё один маркер.</remarks>
  /// <param name="lineNumber">Номер строки.</param>
  /// <param name="textView">Текущее представление AvalonEdit.</param>
  /// <param name="verticalOffset">Текущая координата по вертикали.</param>
  /// <param name="lineHeight">Высота строки.</param>
  /// <param name="drawingContext">Контекст рисования.</param>
  /// <param name="brush">Кисть для отрисовки.</param>
  /// <param name="isEnabled">Отрисовка дополнительного маркера.</param>
  private static void RenderBreakpointMarker(
    int lineNumber,
    TextView textView,
    double verticalOffset,
    double lineHeight,
    DrawingContext drawingContext,
    Brush brush,
    bool isEnabled)
  {
    double top = textView.GetVisualTopByDocumentLine(lineNumber);
    if (double.IsNaN(top)) return;

    double centerY = top - verticalOffset + lineHeight * 0.5;

    drawingContext.DrawEllipse(brush, null, new Point(MarkerCenterX, centerY), 8, 8);

    if (!isEnabled)
      drawingContext.DrawEllipse(BreakpointHoleBrush, null, new Point(MarkerCenterX, centerY), 7, 7);
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
    while (i < text.Length && (text[i] == ' ' || text[i] == '\t')) i++;
    if (i >= text.Length)
      return 0;

    int value = 0;
    for (; i < text.Length; i++)
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
      RemoveBreakpoint(lineNumber, commandNumber, raiseEvents: true);
      return;
    }

    SetBreakpoint(lineNumber, commandNumber, raiseEvents: true);
  }

  /// <summary>
  /// Calculates active command visual range from the command header line to the line before next command header.
  /// </summary>
  /// <param name="startLineZeroBased">Active command start line (0-based).</param>
  private void UpdateActiveRangeFromStart(int startLineZeroBased)
  {
    var document = _textEditor.Document;
    if (document == null || document.LineCount == 0)
    {
      _activeRangeStartLine = -1;
      _activeRangeEndLine = -1;
      return;
    }

    int start = Math.Clamp(startLineZeroBased, 0, document.LineCount - 1);
    int end = document.LineCount - 1;

    for (int lineNumber = start + 2; lineNumber <= document.LineCount; lineNumber++)
    {
      var line = document.GetLineByNumber(lineNumber);
      var text = document.GetText(line);

      if (!CommandHeaderRegex.IsMatch(text))
        continue;

      end = lineNumber - 2;
      break;
    }

    _activeRangeStartLine = start;
    _activeRangeEndLine = Math.Max(start, end);
  }

  /// <summary>
  /// Draws vertical range marker for currently active command.
  /// </summary>
  private static void DrawActiveRange(
    DrawingContext drawingContext,
    TextView textView,
    double verticalOffset,
    double lineHeight,
    int startLineZeroBased,
    int endLineZeroBased,
    double lineX,
    Brush brush)
  {
    int startDocLine = startLineZeroBased + 1;
    int endDocLine = endLineZeroBased + 1;

    double startTop = textView.GetVisualTopByDocumentLine(startDocLine);
    double endTop = textView.GetVisualTopByDocumentLine(endDocLine);
    if (double.IsNaN(startTop) || double.IsNaN(endTop))
      return;

    double y1 = startTop - verticalOffset + ActiveRangeLinePadding;
    double y2 = endTop - verticalOffset + lineHeight - ActiveRangeLinePadding;
    if (y2 <= y1)
      y2 = y1 + 1;

    var pen = new Pen(brush, ActiveRangeLineThickness);
    if (pen.CanFreeze)
      pen.Freeze();

    drawingContext.DrawLine(pen, new Point(lineX, y1), new Point(lineX, y2));
  }

  /// <summary>
  /// Инвалидирует слой фона текстовой области для перерисовки активного диапазона.
  /// </summary>
  private void InvalidateEditorBackground()
  {
    _textEditor.TextArea.TextView.InvalidateLayer(KnownLayer.Background);

    foreach (var margin in _textEditor.TextArea.LeftMargins)
    {
      margin.InvalidateVisual();
    }
  }

  /// <summary>
  /// Фоновый рендерер активного диапазона команды (как в VS): прямоугольник на всю ширину текстовой области.
  /// </summary>
  private sealed class ActiveCommandRangeBackgroundRenderer : IBackgroundRenderer
  {
    private readonly ExecutionGlyphMargin _owner;

    public ActiveCommandRangeBackgroundRenderer(ExecutionGlyphMargin owner)
    {
      _owner = owner;
    }

    public KnownLayer Layer => KnownLayer.Background;

    public void Draw(TextView textView, DrawingContext drawingContext)
    {
      if (!textView.VisualLinesValid)
        return;

      _owner.RefreshThemeBrushes();

      if (!_owner.TryGetActiveRange(out int startDocLine, out int endDocLine))
        return;

      double verticalOffset = textView.ScrollOffset.Y;
      double lineHeight = textView.DefaultLineHeight;

      double startTop = textView.GetVisualTopByDocumentLine(startDocLine);
      double endTop = textView.GetVisualTopByDocumentLine(endDocLine);
      if (double.IsNaN(startTop) || double.IsNaN(endTop))
        return;

      double y1 = startTop - verticalOffset + ActiveRangeLinePadding;
      double y2 = endTop - verticalOffset + lineHeight - ActiveRangeLinePadding;
      if (y2 <= y1)
        return;

      var brush = _owner.ActiveRangeBackgroundBrush;
      if (brush == null)
        return;

      drawingContext.DrawRectangle(
        brush,
        null,
        new Rect(
          -ActiveRangeBackgroundLeftOverflow,
          y1,
          textView.ActualWidth + ActiveRangeBackgroundLeftOverflow,
          y2 - y1));
    }
  }

  /// <summary>
  /// Фоновый рендерер диапазонов команд с точками останова.
  /// Рисует полупрозрачный прямоугольник на всю ширину текстовой области.
  /// </summary>
  private sealed class BreakpointRangeBackgroundRenderer : IBackgroundRenderer
  {
    private readonly ExecutionGlyphMargin _owner;

    public BreakpointRangeBackgroundRenderer(ExecutionGlyphMargin owner)
    {
      _owner = owner;
    }

    public KnownLayer Layer => KnownLayer.Background;

    public void Draw(TextView textView, DrawingContext drawingContext)
    {
      if (!textView.VisualLinesValid || !_owner.BreakpointsVisible || _owner.BreakpointLines.Count == 0)
        return;

      var document = _owner._textEditor.Document;
      if (document == null)
        return;

      _owner.RefreshThemeBrushes();
      var brush = _owner.BreakpointRangeBackgroundBrush;
      if (brush == null)
        return;

      double verticalOffset = textView.ScrollOffset.Y;
      double lineHeight = textView.DefaultLineHeight;
      var renderedStartLines = new HashSet<int>();

      for (int i = 0; i < _owner.BreakpointLines.Count; i++)
      {
        var anchor = _owner.BreakpointLines[i];
        int startDocLine = document.GetLineByOffset(anchor.Offset).LineNumber;
        if (!renderedStartLines.Add(startDocLine))
          continue;

        if (!_owner.TryGetCommandLineRange(startDocLine, out int endDocLine))
          continue;

        double startTop = textView.GetVisualTopByDocumentLine(startDocLine);
        double endTop = textView.GetVisualTopByDocumentLine(endDocLine);
        if (double.IsNaN(startTop) || double.IsNaN(endTop))
          continue;

        double y1 = startTop - verticalOffset + ActiveRangeLinePadding;
        double y2 = endTop - verticalOffset + lineHeight - ActiveRangeLinePadding;
        if (y2 <= y1)
          continue;

        drawingContext.DrawRectangle(
          brush,
          null,
          new Rect(
            -ActiveRangeBackgroundLeftOverflow,
            y1,
            textView.ActualWidth + ActiveRangeBackgroundLeftOverflow,
            y2 - y1));
      }
    }
  }

  /// <summary>
  /// Марджин номеров строк с фоновой заливкой активной команды.
  /// </summary>
  private sealed class ActiveRangeLineNumberMargin : LineNumberMargin
  {
    private readonly ExecutionGlyphMargin _owner;

    public ActiveRangeLineNumberMargin(ExecutionGlyphMargin owner)
    {
      _owner = owner;
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
      DrawActiveRangeBackground(drawingContext);
      base.OnRender(drawingContext);
    }

    private void DrawActiveRangeBackground(DrawingContext drawingContext)
    {
      if (TextView == null || !TextView.VisualLinesValid)
        return;

      _owner.RefreshThemeBrushes();

      if (!_owner.TryGetActiveRange(out int startDocLine, out int endDocLine))
        return;

      double verticalOffset = TextView.ScrollOffset.Y;
      double lineHeight = TextView.DefaultLineHeight;

      double startTop = TextView.GetVisualTopByDocumentLine(startDocLine);
      double endTop = TextView.GetVisualTopByDocumentLine(endDocLine);
      if (double.IsNaN(startTop) || double.IsNaN(endTop))
        return;

      double y1 = startTop - verticalOffset + ActiveRangeLinePadding;
      double y2 = endTop - verticalOffset + lineHeight - ActiveRangeLinePadding;
      if (y2 <= y1)
        return;

      var brush = _owner.ActiveRangeBackgroundBrush;
      if (brush == null)
        return;

      drawingContext.DrawRectangle(brush, null, new Rect(0, y1, ActualWidth, y2 - y1));
    }
  }

  /// <summary>
  /// Марджин сворачивания с фоновой заливкой активной команды.
  /// </summary>
  private sealed class ActiveRangeFoldingMargin : FoldingMargin
  {
    private const string ChevronDownGeometryData =
      "M4.08 7.6a1.5 1.5 0 0 1 2.12 0l5.66 5.65 5.66-5.65a1.5 1.5 0 1 1 2.12 2.12l-6.72 6.72a1.5 1.5 0 0 1-2.12 0L4.08 9.72a1.5 1.5 0 0 1 0-2.12Z";
    private const double ChevronMarkerScale = 0.9;
    private const double ChevronStrokeThickness = 1.15;
    private const double ExpandedAngle = 0;
    private const double CollapsedAngle = -90;
    private const double RotationStepDegrees = 12;
    private const double RotationSnapEpsilon = 0.1;
    private static readonly Geometry ChevronDownGeometry = CreateChevronDownGeometry();
    private static readonly Type? FoldingMarkerType =
      typeof(FoldingMargin).Assembly.GetType("ICSharpCode.AvalonEdit.Folding.FoldingMarginMarker");
    private static readonly PropertyInfo? MarkerIsExpandedProperty =
      FoldingMarkerType?.GetProperty("IsExpanded", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
    private readonly ExecutionGlyphMargin _owner;
    private readonly Dictionary<UIElement, double> _markerAngles = new();
    private bool _renderLoopAttached;

    public ActiveRangeFoldingMargin(ExecutionGlyphMargin owner)
    {
      _owner = owner;
      AddHandler(QueryCursorEvent, new QueryCursorEventHandler(OnAnyQueryCursor), true);
      Unloaded += (_, _) => DetachRenderLoop();
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
      DrawActiveRangeBackground(drawingContext);
      base.OnRender(drawingContext);
      DrawChevronMarkers(drawingContext);
    }

    private void DrawActiveRangeBackground(DrawingContext drawingContext)
    {
      if (TextView == null || !TextView.VisualLinesValid)
        return;

      _owner.RefreshThemeBrushes();

      if (!_owner.TryGetActiveRange(out int startDocLine, out int endDocLine))
        return;

      double verticalOffset = TextView.ScrollOffset.Y;
      double lineHeight = TextView.DefaultLineHeight;

      double startTop = TextView.GetVisualTopByDocumentLine(startDocLine);
      double endTop = TextView.GetVisualTopByDocumentLine(endDocLine);
      if (double.IsNaN(startTop) || double.IsNaN(endTop))
        return;

      double y1 = startTop - verticalOffset + ActiveRangeLinePadding;
      double y2 = endTop - verticalOffset + lineHeight - ActiveRangeLinePadding;
      if (y2 <= y1)
        return;

      var brush = _owner.ActiveRangeBackgroundBrush;
      if (brush == null)
        return;

      drawingContext.DrawRectangle(brush, null, new Rect(0, y1, ActualWidth, y2 - y1));
    }

    private static Geometry CreateChevronDownGeometry()
    {
      var geometry = Geometry.Parse(ChevronDownGeometryData);
      if (geometry.CanFreeze)
        geometry.Freeze();

      return geometry;
    }

    private void DrawChevronMarkers(DrawingContext drawingContext)
    {
      int childCount = VisualTreeHelper.GetChildrenCount(this);
      if (childCount == 0)
      {
        _markerAngles.Clear();
        DetachRenderLoop();
        return;
      }

      var activeMarkers = new HashSet<UIElement>();
      bool hasAnimatedMarkers = false;

      for (int i = 0; i < childCount; i++)
      {
        if (VisualTreeHelper.GetChild(this, i) is not UIElement markerElement)
          continue;

        markerElement.Opacity = 0;

        if (markerElement is not Visual markerVisual)
          continue;

        var markerSize = markerElement.RenderSize;
        if (markerSize.Width <= 0 || markerSize.Height <= 0)
          continue;

        Vector markerOffset = VisualTreeHelper.GetOffset(markerVisual);
        var markerRect = new Rect(markerOffset.X, markerOffset.Y, markerSize.Width, markerSize.Height);

        activeMarkers.Add(markerElement);

        bool isExpanded = GetMarkerIsExpanded(markerElement);
        double targetAngle = isExpanded ? ExpandedAngle : CollapsedAngle;
        double currentAngle = _markerAngles.TryGetValue(markerElement, out double knownAngle)
          ? knownAngle
          : targetAngle;
        double renderedAngle = AnimateAngle(currentAngle, targetAngle, out bool isAnimating);
        _markerAngles[markerElement] = renderedAngle;
        hasAnimatedMarkers |= isAnimating;

        Brush markerBrush = ResolveChevronBrush(markerElement);
        DrawChevronGeometry(drawingContext, markerRect, markerBrush, renderedAngle);
      }

      RemoveDeadMarkers(activeMarkers);

      if (hasAnimatedMarkers)
        AttachRenderLoop();
      else
        DetachRenderLoop();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
      base.OnMouseMove(e);
      Cursor = IsPointOverAnyMarker(e.GetPosition(this)) ? Cursors.Hand : null;
    }

    protected override void OnMouseLeave(MouseEventArgs e)
    {
      base.OnMouseLeave(e);
      Cursor = null;
    }

    protected override void OnQueryCursor(QueryCursorEventArgs e)
    {
      if (IsPointOverAnyMarker(e.GetPosition(this)))
      {
        e.Cursor = Cursors.Hand;
        e.Handled = true;
        return;
      }

      base.OnQueryCursor(e);
    }

    private void OnAnyQueryCursor(object sender, QueryCursorEventArgs e)
    {
      if (!IsPointOverAnyMarker(e.GetPosition(this)))
        return;

      e.Cursor = Cursors.Hand;
      e.Handled = true;
    }

    private Brush ResolveChevronBrush(UIElement markerElement)
    {
      // Цвет выделения шеврона = основной цвет текста.
      if (markerElement.IsMouseOver)
        return (Brush?)Application.Current?.Resources["TextEditorFoldingChevronHoverBrush"] ?? Brushes.White;

      return (Brush?)Application.Current?.Resources["TextEditorFoldingChevronBrush"] ?? Brushes.Gray;
    }

    private static bool GetMarkerIsExpanded(UIElement markerElement)
    {
      if (FoldingMarkerType == null
          || MarkerIsExpandedProperty == null
          || !FoldingMarkerType.IsInstanceOfType(markerElement))
        return true;

      return MarkerIsExpandedProperty.GetValue(markerElement) as bool? ?? true;
    }

    private static void DrawChevronGeometry(
      DrawingContext drawingContext,
      Rect markerRect,
      Brush brush,
      double angle)
    {
      if (brush == null)
        return;

      Rect geometryBounds = ChevronDownGeometry.Bounds;
      if (geometryBounds.IsEmpty || geometryBounds.Width <= 0 || geometryBounds.Height <= 0)
        return;

      double markerSize = Math.Min(markerRect.Width, markerRect.Height) * ChevronMarkerScale;
      if (markerSize <= 0)
        return;

      double scale = markerSize / Math.Max(geometryBounds.Width, geometryBounds.Height);
      double iconWidth = geometryBounds.Width * scale;
      double iconHeight = geometryBounds.Height * scale;
      double x = markerRect.X + ((markerRect.Width - iconWidth) * 0.5) - (geometryBounds.X * scale);
      double y = markerRect.Y + ((markerRect.Height - iconHeight) * 0.5) - (geometryBounds.Y * scale);

      var transformGroup = new TransformGroup();
      transformGroup.Children.Add(new ScaleTransform(scale, scale));
      transformGroup.Children.Add(new TranslateTransform(x, y));

      if (Math.Abs(angle) > RotationSnapEpsilon)
      {
        double centerX = markerRect.X + (markerRect.Width * 0.5);
        double centerY = markerRect.Y + (markerRect.Height * 0.5);
        transformGroup.Children.Add(new RotateTransform(angle, centerX, centerY));
      }

      var pen = new Pen(brush, ChevronStrokeThickness)
      {
        StartLineCap = PenLineCap.Round,
        EndLineCap = PenLineCap.Round,
        LineJoin = PenLineJoin.Round
      };

      drawingContext.PushTransform(transformGroup);
      drawingContext.DrawGeometry(brush, pen, ChevronDownGeometry);
      drawingContext.Pop();
    }

    private static double AnimateAngle(double currentAngle, double targetAngle, out bool isAnimating)
    {
      double delta = targetAngle - currentAngle;
      if (Math.Abs(delta) <= RotationSnapEpsilon)
      {
        isAnimating = false;
        return targetAngle;
      }

      isAnimating = true;
      double step = Math.Sign(delta) * Math.Min(Math.Abs(delta), RotationStepDegrees);
      return currentAngle + step;
    }

    private void RemoveDeadMarkers(HashSet<UIElement> activeMarkers)
    {
      if (_markerAngles.Count == 0)
        return;

      var deadMarkers = new List<UIElement>();
      foreach (var marker in _markerAngles.Keys)
      {
        if (!activeMarkers.Contains(marker))
          deadMarkers.Add(marker);
      }

      for (int i = 0; i < deadMarkers.Count; i++)
        _markerAngles.Remove(deadMarkers[i]);
    }

    private bool IsPointOverAnyMarker(Point point)
    {
      int childCount = VisualTreeHelper.GetChildrenCount(this);
      if (childCount == 0)
        return false;

      for (int i = 0; i < childCount; i++)
      {
        if (VisualTreeHelper.GetChild(this, i) is not UIElement markerElement)
          continue;

        if (markerElement is not Visual markerVisual)
          continue;

        var markerSize = markerElement.RenderSize;
        if (markerSize.Width <= 0 || markerSize.Height <= 0)
          continue;

        Vector markerOffset = VisualTreeHelper.GetOffset(markerVisual);
        var markerRect = new Rect(markerOffset.X, markerOffset.Y, markerSize.Width, markerSize.Height);
        if (markerRect.Contains(point))
          return true;
      }

      return false;
    }

    private void AttachRenderLoop()
    {
      if (_renderLoopAttached)
        return;

      CompositionTarget.Rendering += OnCompositionTargetRendering;
      _renderLoopAttached = true;
    }

    private void DetachRenderLoop()
    {
      if (!_renderLoopAttached)
        return;

      CompositionTarget.Rendering -= OnCompositionTargetRendering;
      _renderLoopAttached = false;
    }

    private void OnCompositionTargetRendering(object? sender, EventArgs e)
    {
      InvalidateVisual();
    }
  }

  protected override void OnRender(DrawingContext drawingContext)
  {
    base.OnRender(drawingContext);
    RefreshThemeBrushes();

    drawingContext.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, ActualWidth, ActualHeight));

    if (TextView == null || !TextView.VisualLinesValid) return;

    TextView.EnsureVisualLines();

    double verticalOffset = TextView.ScrollOffset.Y;
    double lineHeight = TextView.DefaultLineHeight;

    if (_activeRangeStartLine >= 0 && _activeRangeEndLine >= _activeRangeStartLine)
    {
      // Keep the stripe on the right edge of this margin:
      // arrow (left) -> stripe -> line numbers (next margin).
      double activeRangeLineX = Math.Max(0, ActualWidth - (ActiveRangeLineThickness * 0.5));
      DrawActiveRange(
        drawingContext,
        TextView,
        verticalOffset,
        lineHeight,
        _activeRangeStartLine,
        _activeRangeEndLine,
        activeRangeLineX,
        ActiveRangeBrush);
    }

    if (BreakpointsVisible && BreakpointLines.Count != 0)
    {
      var doc = _textEditor.Document;

      for (int i = 0; i < BreakpointLines.Count; i++)
      {
        int lineNumber = doc.GetLineByOffset(BreakpointLines[i].Offset).LineNumber;

        int cmd = BreakpointCommandsNumbers[i];
        bool enabled = !_disabledBreakpoints.Contains(cmd);

        RenderBreakpointMarker(lineNumber, TextView, verticalOffset, lineHeight, drawingContext, BreakpointBrush, enabled);
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
    var doc = _textEditor.Document;
    if (doc == null || lineNumber < 1 || lineNumber > doc.LineCount)
      return;

    if (!TryResolveCommandHeaderLine(lineNumber, out int commandHeaderLine))
      return;

    if (!IsBreakpointAllowedLine(commandHeaderLine))
      return;

    ToggleBreakpointAtLine(commandHeaderLine);
    InvalidateVisual();
  }

  /// <summary>
  /// Проверяет, можно ли поставить брейкпоинт на строке команды с учётом разрешённого списка.
  /// Поддерживает как 1-based, так и 0-based списки линий.
  /// </summary>
  private bool IsBreakpointAllowedLine(int commandHeaderLine)
  {
    if (_rightBreakpoints.Count == 0)
      return true;

    return _rightBreakpoints.Contains(commandHeaderLine)
      || _rightBreakpoints.Contains(commandHeaderLine - 1);
  }

  /// <summary>
  /// Находит заголовок команды для произвольной строки: сама строка, если это заголовок,
  /// либо ближайший заголовок выше по документу.
  /// </summary>
  private bool TryResolveCommandHeaderLine(int lineNumber, out int commandHeaderLine)
  {
    var doc = _textEditor.Document;
    commandHeaderLine = -1;

    if (doc == null || lineNumber < 1 || lineNumber > doc.LineCount)
      return false;

    for (int currentLine = lineNumber; currentLine >= 1; currentLine--)
    {
      var line = doc.GetLineByNumber(currentLine);
      var text = doc.GetText(line);
      if (!CommandHeaderRegex.IsMatch(text))
        continue;

      commandHeaderLine = currentLine;
      return true;
    }

    return false;
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
