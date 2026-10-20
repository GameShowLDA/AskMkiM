using Ask.Core.Shared.Interfaces.UiInterfaces;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace Ask.UI.Features.FileComparison.Views;

/// <summary>Подключает поиск к обеим сторонам активного сравнения без изменения документов.</summary>
public partial class FileCompareSelectionControl : ITextSearchTarget
{
  /// <inheritdoc />
  public IReadOnlyList<SearchDocument> GetSearchDocuments() => ActiveComparison == null
    ? Array.Empty<SearchDocument>()
    : new[]
    {
      new SearchDocument("source", "Слева: " + ActiveComparison.SourceFile.DisplayName, ActiveComparison.SourceFile.TextContent),
      new SearchDocument("target", "Справа: " + ActiveComparison.TargetFile.DisplayName, ActiveComparison.TargetFile.TextContent),
    };

  /// <inheritdoc />
  public void HighlightSearchResults(IReadOnlyList<TextSearchMatch> matches, TextSearchMatch? current)
  {
    if (ActiveComparison == null) return;
    ApplySearchRanges(ActiveComparison.SourceLines, "source", ActiveComparison.SourceFile.TextContent, matches, current);
    ApplySearchRanges(ActiveComparison.TargetLines, "target", ActiveComparison.TargetFile.TextContent, matches, current);
  }

  /// <inheritdoc />
  public void NavigateToSearchResult(TextSearchMatch match)
  {
    if (ActiveComparison == null) return;
    var lines = match.DocumentId == "source" ? ActiveComparison.SourceLines : ActiveComparison.TargetLines;
    for (int index = 0; index < lines.Count; index++)
    {
      if (lines[index].OriginalLineNumber != match.LineNumber) continue;
      ComparisonRowsListBox.ScrollIntoView(ActiveComparison.Rows[index]);
      ComparisonRowsListBox.UpdateLayout();
      if (ComparisonRowsListBox.ItemContainerGenerator.ContainerFromIndex(index) is DependencyObject container
        && FindSearchTextBlock(container, lines[index]) is TextBlock textBlock)
      {
        int offset = match.Column;
        foreach (var run in textBlock.Inlines.OfType<Run>())
        {
          if (offset < run.Text.Length)
          {
            var pointer = run.ContentStart.GetPositionAtOffset(offset);
            var rectangle = pointer?.GetCharacterRect(LogicalDirection.Forward) ?? Rect.Empty;
            if (!rectangle.IsEmpty) textBlock.BringIntoView(rectangle);
            break;
          }
          offset -= run.Text.Length;
        }
      }
      break;
    }
  }

  /// <summary>Находит текст нужной стороны внутри виртуализированной пары строк.</summary>
  /// <param name="root">Контейнер пары строк.</param>
  /// <param name="line">Модель строки нужной стороны.</param>
  /// <returns>Элемент текста либо отсутствие созданного элемента.</returns>
  private static TextBlock? FindSearchTextBlock(DependencyObject root, DiffLineViewModel line)
  {
    if (root is TextBlock text && ReferenceEquals(DiffTextHighlight.GetLine(text), line)) return text;
    for (int index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
    {
      var found = FindSearchTextBlock(VisualTreeHelper.GetChild(root, index), line);
      if (found != null) return found;
    }
    return null;
  }

  /// <summary>Распределяет диапазоны поиска по исходным строкам, включая многострочные совпадения.</summary>
  /// <param name="lines">Выровненные строки стороны сравнения.</param>
  /// <param name="documentId">Идентификатор стороны.</param>
  /// <param name="text">Исходный текст с сохранёнными разделителями строк.</param>
  /// <param name="matches">Совпадения текущего запроса.</param>
  /// <param name="current">Активное совпадение.</param>
  private static void ApplySearchRanges(IReadOnlyList<DiffLineViewModel> lines, string documentId, string text,
    IReadOnlyList<TextSearchMatch> matches, TextSearchMatch? current)
  {
    var starts = new List<int> { 0 };
    for (int index = 0; index < text.Length; index++)
    {
      if (text[index] == '\r')
      {
        if (index + 1 < text.Length && text[index + 1] == '\n') index++;
        starts.Add(index + 1);
      }
      else if (text[index] == '\n') starts.Add(index + 1);
    }
    var documentMatches = matches.Where(match => match.DocumentId == documentId).ToArray();
    int firstMatch = 0;
    foreach (var line in lines)
    {
      var ranges = new List<DiffSearchRange>();
      if (line.OriginalLineNumber is int number && number > 0 && number <= starts.Count)
      {
        int start = starts[number - 1], end = start + line.Text.Length;
        while (firstMatch < documentMatches.Length && documentMatches[firstMatch].StartOffset + documentMatches[firstMatch].Length <= start)
          firstMatch++;
        for (int index = firstMatch; index < documentMatches.Length && documentMatches[index].StartOffset < end; index++)
        {
          var match = documentMatches[index];
          int rangeStart = Math.Max(start, match.StartOffset), rangeEnd = Math.Min(end, match.StartOffset + match.Length);
          if (rangeEnd > rangeStart) ranges.Add(new DiffSearchRange(rangeStart - start, rangeEnd - rangeStart, match == current));
        }
      }
      line.SearchRanges = ranges;
    }
  }
}

/// <summary>Диапазон подсветки поиска внутри строки сравнения.</summary>
/// <param name="Start">Начальное смещение внутри строки.</param>
/// <param name="Length">Длина диапазона.</param>
/// <param name="IsCurrent">Признак выбранного совпадения.</param>
public sealed record DiffSearchRange(int Start, int Length, bool IsCurrent);
