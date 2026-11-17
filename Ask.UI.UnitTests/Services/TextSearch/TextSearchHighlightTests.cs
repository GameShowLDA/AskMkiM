using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.UI.Features.FileComparison.Views;
using Ask.UI.Shared.Search;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace Ask.UI.UnitTests.Services.TextSearch;

/// <summary>Проверяет совместное отображение поиска и различий в WPF.</summary>
public sealed class TextSearchHighlightTests
{
  /// <summary>Сохраняет исходный текст и даёт полупрозрачный фон только активному совпадению.</summary>
  [Fact]
  public void Highlight_FillsOnlyCurrentMatch()
  {
    RunInSta(() =>
    {
      var block = new TextBlock();
      DiffTextHighlight.SetLine(block, new DiffLineViewModel(DiffLineKind.Unchanged, DiffPaneSide.Source, 1, 1, "AB AB"));
      DiffTextHighlight.SetSearchRanges(block, new[] { new DiffSearchRange(0, 2, false), new DiffSearchRange(3, 2, true) });
      var runs = block.Inlines.OfType<Run>().ToArray();
      Assert.Null(runs[0].Background);
      var background = Assert.IsType<System.Windows.Media.SolidColorBrush>(runs.Last().Background);
      Assert.Equal(128, background.Color.A);
      Assert.Equal("AB AB", string.Concat(runs.Select(run => run.Text)));
      DiffTextHighlight.SetSearchRanges(block, Array.Empty<DiffSearchRange>());
      Assert.All(block.Inlines.OfType<Run>(), run => Assert.Null(run.Background));
    });
  }

  /// <summary>Сохраняет различия после наложения и очистки поиска и повторного использования элемента.</summary>
  [Fact]
  public void Highlight_PreservesDiffAndClearsRecycledContent()
  {
    RunInSta(() =>
    {
      var line = new DiffLineViewModel(DiffLineKind.Modified, DiffPaneSide.Source, 1, 1, "ABC",
        new[] { new DiffTextSegment("A", false), new DiffTextSegment("B", true), new DiffTextSegment("C", false) });
      var block = new TextBlock();
      DiffTextHighlight.SetLine(block, line);
      Assert.Equal("ABC", string.Concat(block.Inlines.OfType<Run>().Select(run => run.Text)));
      DiffTextHighlight.SetSearchRanges(block, new[] { new DiffSearchRange(0, 2, true) });
      var changed = Assert.Single(block.Inlines.OfType<Run>(), run => run.Text == "B");
      Assert.Equal(FontWeights.Bold, changed.FontWeight);
      Assert.NotEmpty(changed.TextDecorations);
      DiffTextHighlight.SetSearchRanges(block, Array.Empty<DiffSearchRange>());
      changed = Assert.Single(block.Inlines.OfType<Run>(), run => run.Text == "B");
      Assert.Same(DiffBrushes.ChangedCharacterBackground, changed.Background);
      Assert.Empty(changed.TextDecorations);
      DiffTextHighlight.SetLine(block, new DiffLineViewModel(DiffLineKind.Unchanged, DiffPaneSide.Source, 2, 2, "Обычная строка"));
      Assert.Equal("Обычная строка", string.Concat(block.Inlines.OfType<Run>().Select(run => run.Text)));
      Assert.All(block.Inlines.OfType<Run>(), run => Assert.Null(run.Background));
      DiffTextHighlight.SetLine(block, null);
      Assert.Empty(block.Inlines);
    });
  }

  /// <summary>Разрешает подключение поиска через attached property и модель компонента.</summary>
  [Fact]
  public void Target_ResolvesAttachedAdapterAndDataContext()
  {
    RunInSta(() =>
    {
      var element = new Border();
      var target = new ReadOnlyTarget();
      element.DataContext = target;
      Assert.Same(target, SearchTarget.Resolve(element));
      var attached = new ReadOnlyTarget();
      SearchTarget.SetTarget(element, attached);
      Assert.Same(attached, SearchTarget.Resolve(element));
      SearchTarget.SetTarget(element, null);
      Assert.Same(target, SearchTarget.Resolve(element));
      element.DataContext = null;
      Assert.Null(SearchTarget.Resolve(element));
    });
  }

  private static void RunInSta(Action action)
  {
    Exception? exception = null;
    var thread = new Thread(() => { try { action(); } catch (Exception error) { exception = error; } });
    thread.SetApartmentState(ApartmentState.STA);
    thread.Start();
    Assert.True(thread.Join(TimeSpan.FromSeconds(15)));
    if (exception != null) throw new InvalidOperationException("Проверка WPF завершилась ошибкой.", exception);
  }

  private sealed class ReadOnlyTarget : ITextSearchTarget
  {
    public IReadOnlyList<SearchDocument> GetSearchDocuments() => Array.Empty<SearchDocument>();
    public void HighlightSearchResults(IReadOnlyList<TextSearchMatch> matches, TextSearchMatch? current) { }
    public void NavigateToSearchResult(TextSearchMatch match) { }
  }
}
