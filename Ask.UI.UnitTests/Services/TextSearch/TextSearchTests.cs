using Ask.Core.Services.TextSearch;
using Ask.Core.Shared.Interfaces.UiInterfaces;

namespace Ask.UI.UnitTests.Services.TextSearch;

/// <summary>Проверяет общий поиск и сессию без зависимости от редактора.</summary>
public sealed class TextSearchTests
{
  /// <summary>Сохраняет буквальную трактовку запроса, регистр и границы слов.</summary>
  /// <param name="query">Текст запроса.</param>
  /// <param name="matchCase">Признак учёта регистра.</param>
  /// <param name="wholeWord">Признак целого слова.</param>
  /// <param name="count">Количество ожидаемых совпадений.</param>
  [Theory]
  [InlineData("ток", false, false, 3)]
  [InlineData("ток", false, true, 2)]
  [InlineData("ток", true, true, 1)]
  [InlineData("[1].*", false, false, 1)]
  [InlineData("", false, false, 0)]
  public void Engine_RespectsOptions(string query, bool matchCase, bool wholeWord, int count)
  {
    var results = TextSearchEngine.Find(new[] { new SearchDocument("id", "Файл", "Ток ток поток [1].*") },
      new TextSearchQuery(query, matchCase, wholeWord));
    Assert.Equal(count, results.Count);
  }

  /// <summary>Сохраняет координаты при разных переводах строки и многострочном запросе.</summary>
  [Fact]
  public void Engine_PreservesOffsetsAndLineNumbers()
  {
    var documents = new[] { new SearchDocument("id", "Файл", "A\r\nB\nC\rD") };
    var results = TextSearchEngine.Find(documents, new TextSearchQuery("B\nC"));
    var result = Assert.Single(results);
    Assert.Equal(new TextSearchMatch("id", 3, 3, 2, 0), result);
    Assert.Equal(4, Assert.Single(TextSearchEngine.Find(documents, new TextSearchQuery("D"))).LineNumber);
    Assert.All(TextSearchEngine.Find(documents, new TextSearchQuery("\n")), match => Assert.True(match.Column >= 0));
  }

  /// <summary>Различает документы с одинаковыми отображаемыми именами.</summary>
  [Fact]
  public void Engine_UsesIdsInsteadOfTitles()
  {
    var results = TextSearchEngine.Find(new[]
    {
      new SearchDocument("left", "test.txt", "X"), new SearchDocument("right", "test.txt", "X"),
    }, new TextSearchQuery("X"));
    Assert.Equal(new[] { "left", "right" }, results.Select(match => match.DocumentId));
    Assert.Throws<ArgumentException>(() => TextSearchEngine.Find(new[]
    {
      new SearchDocument("id", "one", "X"), new SearchDocument("id", "two", "X"),
    }, new TextSearchQuery("X")));
  }

  /// <summary>Учитывает отмену запроса до начала вычислений.</summary>
  [Fact]
  public void Engine_RespectsCancellation()
  {
    using var cancellation = new CancellationTokenSource();
    cancellation.Cancel();
    Assert.Throws<OperationCanceledException>(() => TextSearchEngine.Find(Array.Empty<SearchDocument>(),
      new TextSearchQuery("X"), cancellation.Token));
  }

  /// <summary>Переходит между документами и циклически обходит совпадения в выбранной области.</summary>
  [Fact]
  public async Task Session_NavigatesScopesAndRefreshesChangedText()
  {
    var target = new MemoryTarget();
    using var session = new TextSearchSession(target);
    var query = new TextSearchQuery("X");
    await session.SearchAsync(query, TextSearchAction.Next);
    Assert.Equal("left", session.Current?.DocumentId);
    await session.SearchAsync(query, TextSearchAction.Next);
    Assert.Equal("right", session.Current?.DocumentId);
    await session.SearchAsync(query, TextSearchAction.Next);
    Assert.Equal("left", session.Current?.DocumentId);
    await session.SearchAsync(query, TextSearchAction.Previous);
    Assert.Equal("right", session.Current?.DocumentId);
    await session.SearchAsync(query, TextSearchAction.FindAll, "right");
    Assert.Single(session.Matches);
    target.Texts["right"] = "XX";
    await session.SearchAsync(query, TextSearchAction.Next, "right");
    Assert.Equal(2, session.Matches.Count);
    Assert.Equal(0, session.CurrentIndex);
    Assert.Equal(session.Current, target.Current);
  }

  /// <summary>Находит совпадение в другом документе, когда первый документ пуст.</summary>
  [Fact]
  public async Task Session_SkipsDocumentsWithoutMatchesAndClearsHighlights()
  {
    var target = new MemoryTarget();
    target.Texts["left"] = "";
    var session = new TextSearchSession(target);
    await session.SearchAsync(new TextSearchQuery("X"), TextSearchAction.Previous);
    Assert.Equal("right", session.Current?.DocumentId);
    await session.SearchAsync(new TextSearchQuery("missing"), TextSearchAction.Next);
    Assert.Empty(session.Matches);
    Assert.Null(target.Current);
    session.Dispose();
    Assert.Empty(target.Highlighted);
    await Assert.ThrowsAsync<ObjectDisposedException>(() => session.SearchAsync(new TextSearchQuery("X"), TextSearchAction.Next));
  }

  /// <summary>Заменяет диапазоны с конца и ограничивает изменения выбранным документом.</summary>
  [Fact]
  public async Task Session_ReplacesWithinSelectedScope()
  {
    var target = new MemoryTarget();
    target.Texts["left"] = "XX";
    using var session = new TextSearchSession(target);
    await session.ReplaceAsync(new TextSearchQuery("X"), "long", true, "left");
    Assert.Equal("longlong", target.Texts["left"]);
    Assert.Equal("X", target.Texts["right"]);
    Assert.Empty(session.Matches);
    await session.ReplaceAsync(new TextSearchQuery("X"), "Y", false, "right");
    Assert.Equal("Y", target.Texts["right"]);
  }

  /// <summary>При новом запросе отменяет устаревший результат.</summary>
  [Fact]
  public async Task Session_LatestRequestWins()
  {
    var target = new MemoryTarget();
    target.Texts["left"] = new string('X', 500_000);
    using var session = new TextSearchSession(target);
    var first = session.SearchAsync(new TextSearchQuery("X"), TextSearchAction.Next);
    var second = session.SearchAsync(new TextSearchQuery("missing"), TextSearchAction.FindAll);
    await Task.WhenAll(first, second);
    Assert.Empty(session.Matches);
    Assert.Empty(target.Highlighted);
  }

  private sealed class MemoryTarget : IReplaceTextSearchTarget
  {
    public Dictionary<string, string> Texts { get; } = new() { ["left"] = "X", ["right"] = "X" };
    public IReadOnlyList<TextSearchMatch> Highlighted { get; private set; } = Array.Empty<TextSearchMatch>();
    public TextSearchMatch? Current { get; private set; }
    public IReadOnlyList<SearchDocument> GetSearchDocuments() => Texts.Select(pair => new SearchDocument(pair.Key, "Файл", pair.Value)).ToArray();
    public void HighlightSearchResults(IReadOnlyList<TextSearchMatch> matches, TextSearchMatch? current) { Highlighted = matches; Current = current; }
    public void NavigateToSearchResult(TextSearchMatch match) => Current = match;
    public void ReplaceSearchResult(TextSearchMatch match, string replacement) =>
      Texts[match.DocumentId] = Texts[match.DocumentId].Remove(match.StartOffset, match.Length).Insert(match.StartOffset, replacement);
  }
}
