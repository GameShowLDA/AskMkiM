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

    if (string.IsNullOrEmpty(query.Text)) return Array.Empty<TextSearchMatch>();
    var regex = CreateRegex(query);
    if (documents.Count > 1 && documents.Sum(document => (long)document.Text.Length) >= 1_000_000)
    {
      var batches = new IReadOnlyList<TextSearchMatch>[documents.Count];
      Parallel.For(0, documents.Count, new ParallelOptions
      {
        CancellationToken = cancellationToken,
        MaxDegreeOfParallelism = Math.Min(4, Environment.ProcessorCount)
      }, index => batches[index] = FindDocument(documents[index], regex, cancellationToken));
      var combined = new List<TextSearchMatch>(batches.Sum(batch => batch.Count));
      foreach (var batch in batches) combined.AddRange(batch);
      return combined;
    }
    var results = new List<TextSearchMatch>();
    foreach (var document in documents) results.AddRange(FindDocument(document, regex, cancellationToken));
    return results;
  }

  private static Regex CreateRegex(TextSearchQuery query)
  {
    string pattern = query.UseRegex ? query.Text : Regex.Escape(query.Text);
    if (query.WholeWord) pattern = @"\b(?:" + pattern + @")\b";
    var options = query.MatchCase ? RegexOptions.None : RegexOptions.IgnoreCase;
    if (query.UseRegex) options |= RegexOptions.Multiline;
    return new Regex(pattern, options, TimeSpan.FromMilliseconds(500));
  }

  /// <summary>Expands regex replacement groups against immutable original documents before editing.</summary>
  public static IReadOnlyList<string> PrepareReplacements(IReadOnlyList<SearchDocument> documents,
    TextSearchQuery query, IReadOnlyList<TextSearchMatch> matches, string replacement)
  {
    if (!query.UseRegex) return Enumerable.Repeat(replacement, matches.Count).ToArray();
    var regex = CreateRegex(query);
    var texts = documents.ToDictionary(document => document.Id, document => document.Text);
    return matches.Select(match =>
    {
      var actual = regex.Match(texts[match.DocumentId], match.StartOffset);
      if (!actual.Success || actual.Index != match.StartOffset || actual.Length != match.Length)
        throw new InvalidOperationException("Результаты поиска устарели. Повторите поиск.");
      return actual.Result(replacement);
    }).ToArray();
  }

  private static IReadOnlyList<TextSearchMatch> FindDocument(SearchDocument document, Regex regex, CancellationToken cancellationToken)
  {
    cancellationToken.ThrowIfCancellationRequested();
    var results = new List<TextSearchMatch>();
    int position = 0, lineNumber = 1, lineStart = 0;
    // ValueMatch enumeration avoids allocating Match/Group objects for each occurrence.
    foreach (var match in regex.EnumerateMatches(document.Text))
    {
      cancellationToken.ThrowIfCancellationRequested();
      while (position < match.Index)
      {
        if ((position & 0xFFFF) == 0) cancellationToken.ThrowIfCancellationRequested();
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
    return results;
  }
}
