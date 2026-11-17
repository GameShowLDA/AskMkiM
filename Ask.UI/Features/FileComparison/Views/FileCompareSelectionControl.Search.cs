using Ask.Core.Shared.Interfaces.UiInterfaces;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace Ask.UI.Features.FileComparison.Views;

/// <summary>Подключает поиск к обеим сторонам активного сравнения без изменения документов.</summary>
public partial class FileCompareSelectionControl : ITextSearchTarget, ITextSearchScopeProvider
{
  private readonly ComparisonSearchIndex _searchIndex = new();

  /// <inheritdoc />
  public string AllDocumentsTitle => "Обе панели";

  /// <inheritdoc />
  public string? SelectedSearchDocumentId { get; set; }

  /// <inheritdoc />
  public IReadOnlyList<SearchDocument> GetSearchDocuments() => ActiveComparison == null
    ? Array.Empty<SearchDocument>()
    : new[]
    {
      new SearchDocument("source", "Левая — " + ActiveComparison.SourceFile.DisplayName, ActiveComparison.SourceFile.TextContent),
      new SearchDocument("target", "Правая — " + ActiveComparison.TargetFile.DisplayName, ActiveComparison.TargetFile.TextContent),
    };

  /// <inheritdoc />
  public void HighlightSearchResults(IReadOnlyList<TextSearchMatch> matches, TextSearchMatch? current)
    => _searchIndex.Update(ActiveComparison, matches, current);



  /// <inheritdoc />
  public void NavigateToSearchResult(TextSearchMatch match)
  {
    if (ActiveComparison == null) return;
    var lines = match.DocumentId == "source" ? ActiveComparison.SourceLines : ActiveComparison.TargetLines;
    int first = _searchIndex.FindRow(ActiveComparison, match) ?? 0;
    for (int index = first; index < lines.Count; index++)
    {
      if (lines[index].OriginalLineNumber != match.LineNumber) continue;
      ComparisonRowsListBox.ScrollIntoView(ActiveComparison.Rows[index]);
      if (ComparisonRowsListBox.ItemContainerGenerator.ContainerFromIndex(index) == null)
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


}

/// <summary>Диапазон подсветки поиска внутри строки сравнения.</summary>
/// <param name="Start">Начальное смещение внутри строки.</param>
/// <param name="Length">Длина диапазона.</param>
/// <param name="IsCurrent">Признак выбранного совпадения.</param>
public sealed record DiffSearchRange(int Start, int Length, bool IsCurrent);
