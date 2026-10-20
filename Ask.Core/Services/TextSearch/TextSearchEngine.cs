using Ask.Core.Shared.Interfaces.UiInterfaces;
using System.Text.RegularExpressions;

namespace Ask.Core.Services.TextSearch;

/// <summary>
/// Ищет буквальные совпадения в снимках документов без обращения к интерфейсу.
/// </summary>
public static class TextSearchEngine
{
  /// <summary>
  /// Находит совпадения с сохранением порядка документов и исходных смещений.
  /// </summary>
  /// <param name="documents">Снимки документов с уникальными идентификаторами.</param>
  /// <param name="query">Текст и параметры поиска.</param>
  /// <param name="cancellationToken">Токен отмены поиска.</param>
  /// <returns>Неперекрывающиеся диапазоны совпадений.</returns>
  /// <exception cref="ArgumentException">Идентификаторы документов пусты или повторяются.</exception>
  public static IReadOnlyList<TextSearchMatch> Find(
    IReadOnlyList<SearchDocument> documents, TextSearchQuery query, CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    if (documents.Any(document => string.IsNullOrWhiteSpace(document.Id))
      || documents.Select(document => document.Id).Distinct(StringComparer.Ordinal).Count() != documents.Count)
      throw new ArgumentException("Идентификаторы документов поиска должны быть непустыми и уникальными.", nameof(documents));

    var results = new List<TextSearchMatch>();
    if (string.IsNullOrEmpty(query.Text)) return results;
    var escaped = Regex.Escape(query.Text);
    var pattern = query.WholeWord ? $@"\b{escaped}\b" : escaped;
    var regex = new Regex(pattern, query.MatchCase ? RegexOptions.None : RegexOptions.IgnoreCase);
    foreach (var document in documents)
    {
      cancellationToken.ThrowIfCancellationRequested();
      int position = 0, lineNumber = 1, lineStart = 0;
      foreach (Match match in regex.Matches(document.Text))
      {
        cancellationToken.ThrowIfCancellationRequested();
        while (position < match.Index)
        {
          char character = document.Text[position++];
          if (character == '\r')
          {
            if (position < document.Text.Length && document.Text[position] == '\n')
            {
              if (position == match.Index) continue;
              position++;
            }
            lineNumber++;
            lineStart = position;
          }
          else if (character == '\n')
          {
            lineNumber++;
            lineStart = position;
          }
        }
        results.Add(new TextSearchMatch(document.Id, match.Index, match.Length, lineNumber, match.Index - lineStart));
      }
    }
    return results;
  }
}
