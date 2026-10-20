namespace Ask.Core.Shared.Interfaces.UiInterfaces;

/// <summary>
/// Предоставляет документы компонента и отображает результаты поиска в его интерфейсе.
/// Методы вызываются в потоке интерфейса; снимки текста обрабатываются независимо от WPF.
/// </summary>
public interface ITextSearchTarget
{
  /// <summary>
  /// Возвращает актуальные неизменяемые снимки документов с непустыми уникальными в пределах компонента идентификаторами.
  /// </summary>
  /// <returns>Документы в порядке перехода между совпадениями.</returns>
  IReadOnlyList<SearchDocument> GetSearchDocuments();

  /// <summary>
  /// Обновляет подсветку поиска, сохраняя остальные виды выделения компонента.
  /// </summary>
  /// <param name="matches">Все совпадения; пустой список очищает подсветку поиска.</param>
  /// <param name="current">Текущее совпадение или отсутствие выбранного результата.</param>
  void HighlightSearchResults(IReadOnlyList<TextSearchMatch> matches, TextSearchMatch? current);

  /// <summary>
  /// Показывает найденный фрагмент в компоненте.
  /// </summary>
  /// <param name="match">Совпадение в актуальном снимке документа.</param>
  void NavigateToSearchResult(TextSearchMatch match);
}

/// <summary>
/// Дополняет подключение поиска возможностью изменения текста.
/// </summary>
public interface IReplaceTextSearchTarget : ITextSearchTarget
{
  /// <summary>
  /// Заменяет найденный фрагмент актуального документа.
  /// </summary>
  /// <param name="match">Заменяемый диапазон.</param>
  /// <param name="replacement">Новый текст фрагмента.</param>
  void ReplaceSearchResult(TextSearchMatch match, string replacement);
}

/// <summary>
/// Неизменяемый снимок документа для поиска.
/// </summary>
/// <param name="Id">Уникальный идентификатор документа, независимый от отображаемого имени.</param>
/// <param name="Title">Отображаемое имя документа или области.</param>
/// <param name="Text">Полное содержимое документа на момент запроса.</param>
public sealed record SearchDocument(string Id, string Title, string Text);

/// <summary>
/// Параметры буквального поиска текста.
/// </summary>
/// <param name="Text">Искомый текст.</param>
/// <param name="MatchCase">Признак учёта регистра.</param>
/// <param name="WholeWord">Признак поиска целого слова.</param>
public sealed record TextSearchQuery(string Text, bool MatchCase = false, bool WholeWord = false);

/// <summary>
/// Найденный диапазон в снимке документа.
/// </summary>
/// <param name="DocumentId">Идентификатор документа.</param>
/// <param name="StartOffset">Смещение начала диапазона от начала документа.</param>
/// <param name="Length">Длина диапазона.</param>
/// <param name="LineNumber">Номер начальной строки, начиная с единицы.</param>
/// <param name="Column">Смещение внутри начальной строки, начиная с нуля.</param>
public sealed record TextSearchMatch(string DocumentId, int StartOffset, int Length, int LineNumber, int Column);

/// <summary>
/// Действие над результатами текущего запроса.
/// </summary>
public enum TextSearchAction
{
  /// <summary>Переход к следующему совпадению.</summary>
  Next,
  /// <summary>Переход к предыдущему совпадению.</summary>
  Previous,
  /// <summary>Обновление всех совпадений без продвижения текущей позиции.</summary>
  FindAll,
}
