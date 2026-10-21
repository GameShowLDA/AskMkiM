using Ask.Core.Shared.Interfaces.UiInterfaces;

namespace Ask.UI.Features.FileComparison.Views;

/// <summary>Кэш диапазонов строк сравнения; шаг навигации меняет только две группы строк.</summary>
internal sealed class ComparisonSearchIndex
{
  private FileComparisonResult? _rangeComparison;
  private IReadOnlyList<TextSearchMatch>? _rangeMatches;
  private TextSearchMatch? _currentRange;
  private readonly Dictionary<TextSearchMatch, List<(DiffLineViewModel Line, int Start, int Length)>> _matchRanges = new();
  private readonly Dictionary<(string Document, int Line), int> _searchLineIndices = new();

  public void Update(FileComparisonResult? comparison, IReadOnlyList<TextSearchMatch> matches, TextSearchMatch? current)
  {
    if (comparison == null) return;
    if (ReferenceEquals(_rangeComparison, comparison) && ReferenceEquals(_rangeMatches, matches))
    {
      UpdateCurrentRange(_currentRange, false);
      UpdateCurrentRange(current, true);
    }
    else
    {
      _rangeComparison = comparison;
      _rangeMatches = matches;
      _matchRanges.Clear();
      _searchLineIndices.Clear();
      ApplySearchRanges(comparison.SourceLines, "source", comparison.SourceFile.TextContent, matches, current, _matchRanges);
      ApplySearchRanges(comparison.TargetLines, "target", comparison.TargetFile.TextContent, matches, current, _matchRanges);
      for (int index = 0; index < comparison.SourceLines.Count; index++)
      {
        if (comparison.SourceLines[index].OriginalLineNumber is int source) _searchLineIndices[("source", source)] = index;
        if (comparison.TargetLines[index].OriginalLineNumber is int target) _searchLineIndices[("target", target)] = index;
      }
    }
    _currentRange = current;
    }

  public int? FindRow(FileComparisonResult comparison, TextSearchMatch match) =>
    ReferenceEquals(_rangeComparison, comparison) && _searchLineIndices.TryGetValue((match.DocumentId, match.LineNumber), out var row)
      ? row : null;

  private void UpdateCurrentRange(TextSearchMatch? match, bool current)
  {
    if (match == null || !_matchRanges.TryGetValue(match, out var ranges)) return;
    foreach (var range in ranges)
      range.Line.SearchRanges = range.Line.SearchRanges.Select(item => item.Start == range.Start && item.Length == range.Length
        ? item with { IsCurrent = current } : item).ToArray();
  }

  private static void ApplySearchRanges(IReadOnlyList<DiffLineViewModel> lines, string documentId, string text,
    IReadOnlyList<TextSearchMatch> matches, TextSearchMatch? current,
    Dictionary<TextSearchMatch, List<(DiffLineViewModel Line, int Start, int Length)>> rangeMap)
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
          if (rangeEnd > rangeStart)
          {
            ranges.Add(new DiffSearchRange(rangeStart - start, rangeEnd - rangeStart, match == current));
            if (!rangeMap.TryGetValue(match, out var mapped)) rangeMap[match] = mapped = new();
            mapped.Add((line, rangeStart - start, rangeEnd - rangeStart));
          }
        }
      }
      line.SearchRanges = ranges;
    }
  }
}
