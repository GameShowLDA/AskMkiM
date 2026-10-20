using Ask.Core.Shared.Interfaces.UiInterfaces;

namespace Ask.Core.Services.TextSearch;

/// <summary>
/// Управляет поиском и навигацией одного подключённого компонента.
/// Публичные методы вызываются последовательно из потока интерфейса; новый запрос отменяет предыдущий.
/// </summary>
public sealed class TextSearchSession : IDisposable
{
  private readonly ITextSearchTarget _target;
  private CancellationTokenSource? _pending;
  private IReadOnlyList<SearchDocument> _documents = Array.Empty<SearchDocument>();
  private TextSearchQuery? _query;
  private bool _disposed;

  /// <summary>Создаёт сессию подключённого компонента.</summary>
  /// <param name="target">Источник документов и обработчик отображения результатов.</param>
  public TextSearchSession(ITextSearchTarget target) => _target = target;

  /// <summary>Совпадения последнего завершённого запроса.</summary>
  public IReadOnlyList<TextSearchMatch> Matches { get; private set; } = Array.Empty<TextSearchMatch>();

  /// <summary>Индекс текущего совпадения; минус один означает отсутствие результата.</summary>
  public int CurrentIndex { get; private set; } = -1;

  /// <summary>Текущее совпадение.</summary>
  public TextSearchMatch? Current => CurrentIndex >= 0 && CurrentIndex < Matches.Count ? Matches[CurrentIndex] : null;

  /// <summary>Выполняет поиск в актуальных снимках и показывает выбранное совпадение.</summary>
  /// <param name="query">Текст и параметры поиска.</param>
  /// <param name="action">Действие над результатами.</param>
  /// <param name="documentId">Область поиска; отсутствие значения включает все документы.</param>
  /// <returns>Признак применения результата; отменённый запрос возвращает ложь.</returns>
  public async Task<bool> SearchAsync(TextSearchQuery query, TextSearchAction action, string? documentId = null)
  {
    ObjectDisposedException.ThrowIf(_disposed, this);
    _pending?.Cancel();
    using var pending = new CancellationTokenSource();
    _pending = pending;
    try
    {
      var documents = _target.GetSearchDocuments().Where(document => documentId == null || document.Id == documentId).ToArray();
      bool changed = _query != query || !_documents.SequenceEqual(documents);
      var matches = changed
        ? await Task.Run(() => TextSearchEngine.Find(documents, query, pending.Token), pending.Token)
        : Matches;
      if (pending.IsCancellationRequested || _disposed) return false;
      _documents = documents;
      _query = query;
      Matches = matches;
      if (Matches.Count == 0) CurrentIndex = -1;
      else if (changed || CurrentIndex < 0) CurrentIndex = action == TextSearchAction.Previous ? Matches.Count - 1 : 0;
      else if (action == TextSearchAction.Next) CurrentIndex = (CurrentIndex + 1) % Matches.Count;
      else if (action == TextSearchAction.Previous) CurrentIndex = (CurrentIndex + Matches.Count - 1) % Matches.Count;
      _target.HighlightSearchResults(Matches, Current);
      if (Current != null) _target.NavigateToSearchResult(Current);
      return true;
    }
    catch (OperationCanceledException) when (pending.IsCancellationRequested)
    {
      return false;
    }
    finally
    {
      if (ReferenceEquals(_pending, pending)) _pending = null;
    }
  }

  /// <summary>Заменяет выбранное или все найденные вхождения и обновляет результаты.</summary>
  /// <param name="query">Текст и параметры поиска.</param>
  /// <param name="replacement">Текст замены.</param>
  /// <param name="replaceAll">Признак замены всех вхождений.</param>
  /// <param name="documentId">Идентификатор области или все документы.</param>
  /// <returns>Задача обновления результатов.</returns>
  public async Task ReplaceAsync(TextSearchQuery query, string replacement, bool replaceAll, string? documentId = null)
  {
    if (_target is not IReplaceTextSearchTarget editable) return;
    if (!await SearchAsync(query, TextSearchAction.FindAll, documentId)) return;
    if (replaceAll)
    {
      foreach (var match in Matches.Reverse()) editable.ReplaceSearchResult(match, replacement);
    }
    else if (Current != null) editable.ReplaceSearchResult(Current, replacement);
    _query = null;
    await SearchAsync(query, TextSearchAction.FindAll, documentId);
  }

  /// <summary>Отменяет незавершённый запрос и очищает только подсветку поиска.</summary>
  public void Dispose()
  {
    if (_disposed) return;
    _disposed = true;
    _pending?.Cancel();
    _target.HighlightSearchResults(Array.Empty<TextSearchMatch>(), null);
    Matches = Array.Empty<TextSearchMatch>();
    CurrentIndex = -1;
  }
}
