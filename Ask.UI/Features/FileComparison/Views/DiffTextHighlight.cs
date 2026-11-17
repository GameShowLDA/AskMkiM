using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace Ask.UI.Features.FileComparison.Views;

/// <summary>
/// Фрагмент строки с признаком изменённых символов.
/// </summary>
/// <param name="Text">Текст фрагмента.</param>
/// <param name="IsChanged">Признак изменения фрагмента относительно второй стороны.</param>
public sealed record DiffTextSegment(string Text, bool IsChanged);

/// <summary>
/// Отображает фрагменты строки сравнения в TextBlock с выделением изменённых символов.
/// </summary>
public static class DiffTextHighlight
{

  private static readonly DependencyProperty OutlineProperty = DependencyProperty.RegisterAttached(
    "Outline", typeof(SearchOutline), typeof(DiffTextHighlight));

  /// <summary>Диапазоны поиска, отображаемые совместно с посимвольными различиями.</summary>
  public static readonly DependencyProperty SearchRangesProperty = DependencyProperty.RegisterAttached(
    "SearchRanges", typeof(IReadOnlyList<DiffSearchRange>), typeof(DiffTextHighlight),
    new PropertyMetadata(null, OnLineChanged));

  /// <summary>Возвращает диапазоны поиска текстового элемента.</summary>
  /// <param name="element">Текстовый элемент.</param>
  /// <returns>Диапазоны поиска либо отсутствие подсветки.</returns>
  public static IReadOnlyList<DiffSearchRange>? GetSearchRanges(DependencyObject element) =>
    (IReadOnlyList<DiffSearchRange>?)element.GetValue(SearchRangesProperty);

  /// <summary>Задаёт диапазоны поиска текстового элемента.</summary>
  /// <param name="element">Текстовый элемент.</param>
  /// <param name="ranges">Диапазоны поиска.</param>
  public static void SetSearchRanges(DependencyObject element, IReadOnlyList<DiffSearchRange>? ranges) =>
    element.SetValue(SearchRangesProperty, ranges);

  /// <summary>
  /// Строка сравнения, отображаемая набором текстовых фрагментов.
  /// </summary>
  public static readonly DependencyProperty LineProperty = DependencyProperty.RegisterAttached(
    "Line", typeof(DiffLineViewModel), typeof(DiffTextHighlight), new PropertyMetadata(null, OnLineChanged));

  /// <summary>
  /// Возвращает отображаемую строку сравнения.
  /// </summary>
  /// <param name="element">Текстовый элемент.</param>
  /// <returns>Модель строки сравнения.</returns>
  public static DiffLineViewModel? GetLine(DependencyObject element) =>
    (DiffLineViewModel?)element.GetValue(LineProperty);

  /// <summary>
  /// Задаёт отображаемую строку сравнения.
  /// </summary>
  /// <param name="element">Текстовый элемент.</param>
  /// <param name="value">Модель строки сравнения.</param>
  public static void SetLine(DependencyObject element, DiffLineViewModel? value) =>
    element.SetValue(LineProperty, value);

  /// <summary>
  /// Пересоздаёт фрагменты при смене строки, включая повторное использование контейнера.
  /// </summary>
  /// <param name="element">Текстовый элемент.</param>
  /// <param name="args">Предыдущее и новое значения строки.</param>
  private static void OnLineChanged(DependencyObject element, DependencyPropertyChangedEventArgs args)
  {
    if (element is not TextBlock textBlock)
    {
      return;
    }

    textBlock.Inlines.Clear();
    textBlock.Loaded -= OnTextLoaded;
    textBlock.Loaded += OnTextLoaded;
    textBlock.Unloaded -= OnTextUnloaded;
    textBlock.Unloaded += OnTextUnloaded;
    UpdateOutline(textBlock);
    if (GetLine(element) is not DiffLineViewModel line)
    {
      return;
    }

    var ranges = GetSearchRanges(element) ?? Array.Empty<DiffSearchRange>();
    var boundaries = new SortedSet<int> { 0, line.Text.Length };
    int offset = 0;
    foreach (var segment in line.Segments)
    {
      offset += segment.Text.Length;
      boundaries.Add(offset);
    }
    foreach (var range in ranges)
    {
      boundaries.Add(Math.Clamp(range.Start, 0, line.Text.Length));
      boundaries.Add(Math.Clamp(range.Start + range.Length, 0, line.Text.Length));
    }
    var points = boundaries.ToArray();
    int segmentIndex = 0, segmentStart = 0, rangeIndex = 0;
    for (int index = 0; index + 1 < points.Length; index++)
    {
      int start = points[index], length = points[index + 1] - start;
      while (segmentIndex + 1 < line.Segments.Count && segmentStart + line.Segments[segmentIndex].Text.Length <= start)
        segmentStart += line.Segments[segmentIndex++].Text.Length;
      if (line.Segments.Count == 0) break;
      var segment = line.Segments[segmentIndex];
      var run = new Run(line.Text.Substring(start, length));
      if (segment.IsChanged)
      {
        run.Background = DiffBrushes.ChangedCharacterBackground;
        run.FontWeight = FontWeights.Bold;
      }

      while (rangeIndex < ranges.Count && ranges[rangeIndex].Start + ranges[rangeIndex].Length <= start) rangeIndex++;
      if (rangeIndex < ranges.Count && ranges[rangeIndex].Start <= start)
      {
        if (ranges[rangeIndex].IsCurrent)
        {
          var color = (textBlock.TryFindResource("VioletColorSolidColorBrush") as SolidColorBrush)?.Color
            ?? Color.FromRgb(145, 132, 238);
          run.Background = CreateBrush(color.R, color.G, color.B);
        }
        if (segment.IsChanged) run.TextDecorations = TextDecorations.Underline;
      }

      textBlock.Inlines.Add(run);
    }
  }

  /// <summary>Подключает рамки после создания визуального дерева строки.</summary>
  private static void OnTextLoaded(object sender, RoutedEventArgs args) => UpdateOutline((TextBlock)sender);

  /// <summary>Освобождает слой рамок при выгрузке виртуализированной строки.</summary>
  private static void OnTextUnloaded(object sender, RoutedEventArgs args)
  {
    var block = (TextBlock)sender;
    if (block.GetValue(OutlineProperty) is SearchOutline outline)
      AdornerLayer.GetAdornerLayer(block)?.Remove(outline);
    block.ClearValue(OutlineProperty);
  }

  /// <summary>Обновляет рамки, сохраняя текст и переносы строки.</summary>
  private static void UpdateOutline(TextBlock block)
  {
    if (block.GetValue(OutlineProperty) is SearchOutline existing)
    {
      existing.InvalidateVisual();
      return;
    }
    var layer = AdornerLayer.GetAdornerLayer(block);
    if (layer == null) return;
    var outline = new SearchOutline(block);
    block.SetValue(OutlineProperty, outline);
    layer.Add(outline);
  }

  /// <summary>Рисует рамки совпадений поверх текста без изменения его метрик.</summary>
  private sealed class SearchOutline : Adorner
  {


    /// <summary>Создаёт слой рамок строки.</summary>
    /// <param name="block">Текст строки сравнения.</param>
    public SearchOutline(TextBlock block) : base(block) { IsHitTestVisible = false; }

    /// <inheritdoc />
    protected override void OnRender(DrawingContext drawingContext)
    {
      var block = (TextBlock)AdornedElement;
      var ranges = GetSearchRanges(block);
      if (ranges == null || ranges.Count == 0) return;
      Rect pending = Rect.Empty;
      int offset = 0;
      foreach (var run in block.Inlines.OfType<Run>())
      {
        foreach (var range in ranges)
        {
          int start = Math.Max(offset, range.Start), end = Math.Min(offset + run.Text.Length, range.Start + range.Length);
          for (int index = start; index < end; index++)
          {
            var pointer = run.ContentStart.GetPositionAtOffset(index - offset);
            var rect = pointer?.GetCharacterRect(LogicalDirection.Forward) ?? Rect.Empty;
            var endPointer = run.ContentStart.GetPositionAtOffset(index - offset + 1);
            var endRect = endPointer?.GetCharacterRect(LogicalDirection.Backward) ?? Rect.Empty;
            if (!rect.IsEmpty && !endRect.IsEmpty && Math.Abs(rect.Top - endRect.Top) < 0.5)
              rect = new Rect(Math.Min(rect.Left, endRect.Left), rect.Top, Math.Abs(endRect.Left - rect.Left), rect.Height);
            if (!rect.IsEmpty) AddRectangle(rect);
          }
        }
        offset += run.Text.Length;
      }
      Flush();
      void AddRectangle(Rect rect)
      {
        if (!pending.IsEmpty && Math.Abs(pending.Top - rect.Top) < 0.5 && rect.Left <= pending.Right + 1)
          pending.Union(rect);
        else { Flush(); pending = rect; }
      }
      void Flush()
      {
        if (pending.IsEmpty) return;
        var outline = block.TryFindResource("VioletColorSolidColorBrush") as Brush
          ?? new SolidColorBrush(Color.FromRgb(145, 132, 238));
        drawingContext.DrawRoundedRectangle(null, new Pen(outline, 1.5), pending, 3, 3);
        pending = Rect.Empty;
      }
    }
  }

  private static Brush CreateBrush(byte red, byte green, byte blue)
  {
    var brush = new SolidColorBrush(Color.FromArgb(128, red, green, blue));
    brush.Freeze();
    return brush;
  }
}
