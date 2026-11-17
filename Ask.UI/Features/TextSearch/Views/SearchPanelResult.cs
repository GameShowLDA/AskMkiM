using Ask.Core.Shared.Interfaces.UiInterfaces;

namespace Ask.UI.Features.TextSearch.Views;

/// <summary>Совпадение со снимком и безопасным переходом к исходному документу.</summary>
public sealed record SearchPanelResult(SearchDocument Document, TextSearchMatch Match, Func<bool> Navigate)
{
  public int ResultIndex { get; init; } = -1;
  public string FileName => Document.Title;
  public int LineNumber => Match.LineNumber;
  private int LineStart => Math.Max(0, Match.StartOffset - Match.Column);
  private int LineEnd
  {
    get
    {
      int end = Document.Text.IndexOfAny(new[] { '\r', '\n' }, Match.StartOffset);
      return end < 0 ? Document.Text.Length : end;
    }
  }
  public string Before
  {
    get
    {
      int start = Math.Max(LineStart, Match.StartOffset - 28);
      return (start > LineStart ? "… " : "") + Document.Text[start..Match.StartOffset];
    }
  }
  public string Hit => Document.Text.Substring(Match.StartOffset, Math.Min(Match.Length, LineEnd - Match.StartOffset));
  public string After
  {
    get
    {
      int start = Math.Min(LineEnd, Match.StartOffset + Match.Length);
      int end = Math.Min(LineEnd, start + 65);
      return Document.Text[start..end] + (end < LineEnd ? " …" : "");
    }
  }
  public string LineText => Document.Text[LineStart..LineEnd];
}

/// <summary>Материализует только видимые строки большой выдачи, а не все совпадения.</summary>
public sealed class SearchPanelResultCollection : IReadOnlyList<SearchPanelResult>
{
  private readonly Func<int, SearchPanelResult> _factory;
  private readonly Dictionary<int, SearchPanelResult> _visibleRows = new();
  public SearchPanelResultCollection(int count, int documentCount, Func<int, SearchPanelResult> factory)
  {
    Count = count;
    DocumentCount = documentCount;
    _factory = factory;
  }
  public int Count { get; }
  public int DocumentCount { get; }
  public SearchPanelResult this[int index]
  {
    get
    {
      if ((uint)index >= (uint)Count) throw new ArgumentOutOfRangeException(nameof(index));
      if (!_visibleRows.TryGetValue(index, out var row))
      {
        if (_visibleRows.Count >= 64) _visibleRows.Clear();
        _visibleRows[index] = row = _factory(index) with { ResultIndex = index };
      }
      return row;
    }
  }
  public IEnumerator<SearchPanelResult> GetEnumerator()
  {
    for (int index = 0; index < Count; index++) yield return this[index];
  }
  System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}
