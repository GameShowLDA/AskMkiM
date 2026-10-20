using Ask.Core.Services.EventCore.Events;
using Ask.Core.Services.EventCore.Services;
using Ask.Core.Services.TextSearch;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using ICSharpCode.AvalonEdit.Document;
using System.Windows.Controls;
using UI.Components.Invoke;
using UI.Controls;
using UI.Controls.TextEditorControl;
using UI.Windows.WpfDocking.Windows.Docking;

namespace UI.Components.MultiEditorMethods;

public partial class TextSearchManager
{
  private sealed class NavigationDocument
  {
    public required (OpenFileButton page, TextEditorContainer container, DockItem item) Target { get; init; }
    public required TextEditorUI Editor { get; init; }
    public required TextDocument Document { get; init; }
    public required ITextSourceVersion Version { get; init; }
    public required SearchDocument Snapshot { get; set; }
    public IReadOnlyList<TextSearchMatch> Matches { get; set; } = Array.Empty<TextSearchMatch>();
  }

  private readonly Dictionary<TextEditorUI, NavigationDocument> _navigationCache = new();
  private TextSearchQuery? _navigationQuery;
  private NavigationDocument? _navigationDocument;
  private int _navigationIndex = -1;
  private CancellationTokenSource? _navigationPending;
  private long _navigationId;
  private IReadOnlyList<NavigationDocument>? _panelCacheDocuments;
  private IReadOnlyList<Ask.UI.Features.TextSearch.Views.SearchPanelResult>? _panelCacheRows;
  private TextEditorUI? _selectionEditor;
  private TextDocument? _selectionDocument;
  private TextAnchor? _selectionStart;
  private TextAnchor? _selectionEnd;
  private bool _selectionOnly;

  public bool CaptureSearchSelection()
  {
    _selectionOnly = false;
    _selectionEditor = multiEditorControl.GetActiveTextEditor();
    _selectionDocument = _selectionEditor?.TextEditor.Document;
    _selectionStart = _selectionEnd = null;
    if (_selectionEditor == null || _selectionDocument == null
        || _selectionEditor.TextEditor.SelectionLength == 0
        || _selectionEditor.TextArea.Selection is ICSharpCode.AvalonEdit.Editing.RectangleSelection) return false;
    int start = _selectionEditor.TextEditor.SelectionStart;
    _selectionStart = _selectionDocument.CreateAnchor(start);
    _selectionEnd = _selectionDocument.CreateAnchor(start + _selectionEditor.TextEditor.SelectionLength);
    _selectionStart.SurviveDeletion = _selectionEnd.SurviveDeletion = true;
    _selectionStart.MovementType = AnchorMovementType.BeforeInsertion;
    _selectionEnd.MovementType = AnchorMovementType.AfterInsertion;
    return true;
  }

  public void SetSelectionSearch(bool enabled)
  {
    if (_selectionOnly == enabled) return;
    _selectionOnly = enabled;
    ClearAllHighlights();
    ResetNavigationCache();
  }

  public void ReleaseSearchSelection()
  {
    SetSelectionSearch(false);
    _selectionEditor = null;
    _selectionDocument = null;
    _selectionStart = _selectionEnd = null;
  }

  private void ResetNavigationCache()
  {
    _navigationPending?.Cancel();
    _navigationPending = null;
    _navigationCache.Clear();
    _panelCacheDocuments = null;
    _panelCacheRows = null;
    _navigationQuery = null;
    _navigationDocument = null;
    _navigationIndex = -1;
  }

  private bool IsCachedDocumentCurrent(NavigationDocument document) =>
    ReferenceEquals(document.Editor.TextEditor.Document, document.Document)
    && document.Document.Version.CompareAge(document.Version) == 0
    && fileManager.EditorWorkspaceModel.OpenPages.Contains(document.Target.page)
    && document.Target.container.DockManager.DockItems.Contains(document.Target.item);

  private async Task<IReadOnlyList<NavigationDocument>> GetCachedDocumentsAsync(TextSearchQuery query, bool allDocuments)
  {
    if (_navigationQuery != query)
    {
      ClearAllHighlights();
      ResetNavigationCache();
      _navigationQuery = query;
    }
    var targets = GetSearchNavigationTargets();
    var alive = targets.Select(target => target.item.Content is TextEditorUI editor ? editor
      : (target.item.Content as TranslatorItem)?.GetLeftBox().GetTextEditor()).Where(editor => editor != null).ToHashSet();
    foreach (var closed in _navigationCache.Keys.Where(editor => !alive.Contains(editor)).ToArray())
      _navigationCache.Remove(closed);
    if (_selectionOnly)
    {
      targets = targets.Where(target => ReferenceEquals(target.item.Content is TextEditorUI editor ? editor
        : (target.item.Content as TranslatorItem)?.GetLeftBox().GetTextEditor(), _selectionEditor)
        && ReferenceEquals(_selectionEditor?.TextEditor.Document, _selectionDocument)).ToList();
    }
    else if (!allDocuments)
    {
      int active = GetActiveSearchTargetIndex(targets);
      targets = active < 0 ? new() : new() { targets[active] };
    }
    var documents = new List<NavigationDocument>();
    var dirty = new List<NavigationDocument>();
    foreach (var target in targets)
    {
      var editor = target.item.Content is TextEditorUI text ? text
        : (target.item.Content as TranslatorItem)?.GetLeftBox().GetTextEditor();
      if (editor == null) continue;
      if (!_navigationCache.TryGetValue(editor, out var cached) || !IsCachedDocumentCurrent(cached))
      {
        var document = editor.TextEditor.Document;
        cached = new NavigationDocument
        {
          Target = target, Editor = editor, Document = document, Version = document.Version,
          Snapshot = new SearchDocument((++_navigationId).ToString(), target.item.Title, document.Text)
        };
        dirty.Add(cached);
      }
      else if (cached.Snapshot.Title != target.item.Title)
      {
        cached.Snapshot = cached.Snapshot with { Title = target.item.Title };
        _panelCacheDocuments = null;
      }
      documents.Add(cached);
    }
    if (dirty.Count == 0) return documents;
    _navigationPending?.Cancel();
    using var pending = new CancellationTokenSource();
    _navigationPending = pending;
    try
    {
      var snapshots = dirty.Select(document => document.Snapshot).ToArray();
      var matches = await Task.Run(() => TextSearchEngine.Find(snapshots, query, pending.Token), pending.Token);
      if (pending.IsCancellationRequested || _navigationQuery != query || documents.Any(document => !IsCachedDocumentCurrent(document)))
        return Array.Empty<NavigationDocument>();
      var lookup = matches.ToLookup(match => match.DocumentId);
      foreach (var document in dirty)
      {
        document.Matches = lookup[document.Snapshot.Id].Where(match => !_selectionOnly
          || (_selectionStart != null && _selectionEnd != null
              && match.StartOffset >= _selectionStart.Offset
              && match.StartOffset + match.Length <= _selectionEnd.Offset)).ToArray();
        _navigationCache[document.Editor] = document;
      }
      return documents;
    }
    catch (OperationCanceledException) when (pending.IsCancellationRequested) { return Array.Empty<NavigationDocument>(); }
    finally { if (ReferenceEquals(_navigationPending, pending)) _navigationPending = null; }
  }

  private async Task NavigateCachedAsync(TextSearchQuery query, bool allDocuments, bool forward)
  {
    if (string.IsNullOrEmpty(query.Text)) { OnSearchWindowClosing(); return; }
    var documents = await GetCachedDocumentsAsync(query, allDocuments);
    if (documents.Count == 0) return;
    var targets = documents.Select(document => document.Target).ToList();
    int active = GetActiveSearchTargetIndex(targets);
    if (active < 0 && _selectionOnly) active = 0;
    if (active < 0) return;
    var document = documents[active];
    if (document.Matches.Count == 0) document.Editor.MarkerService.SetSearchMarkers(document.Matches, -1);
    int index;
    if (ReferenceEquals(document, _navigationDocument) && _navigationIndex >= 0
        && document.Editor.TextArea.Caret.Offset == document.Matches[_navigationIndex].StartOffset + document.Matches[_navigationIndex].Length)
      index = _navigationIndex + (forward ? 1 : -1);
    else
    {
      // Binary lower bound starts at the caret without a linear scan of the matches.
      int caret = document.Editor.TextArea.Caret.Offset;
      int low = 0, high = document.Matches.Count;
      while (low < high)
      {
        int middle = low + (high - low) / 2;
        if (document.Matches[middle].StartOffset < caret) low = middle + 1;
        else high = middle;
      }
      index = forward ? low : low - 1;
    }
    for (int step = 0; step <= documents.Count; step++)
    {
      if (index >= 0 && index < document.Matches.Count)
      {
        GoToCachedMatch(documents, document, index);
        return;
      }
      active = (active + (forward ? 1 : -1) + documents.Count) % documents.Count;
      document = documents[active];
      index = forward ? 0 : document.Matches.Count - 1;
    }
    EventAggregator.Publish(new SearchEvents.SearchProgressChanged(0, 0));
  }

  public async Task ReplaceCachedAsync(TextSearchQuery query, string replacement, bool allDocuments, bool replaceAll)
  {
    var initialEditor = multiEditorControl.GetActiveTextEditor();
    var documents = await GetCachedDocumentsAsync(query, allDocuments);
    if (!ReferenceEquals(initialEditor, multiEditorControl.GetActiveTextEditor())
        || documents.Count == 0 || documents.Any(document => !IsCachedDocumentCurrent(document))) return;
    if (replaceAll)
    {
      var plannedMatches = documents.SelectMany(document => document.Matches).ToArray();
      var snapshots = documents.Select(document => document.Snapshot).ToArray();
      var replacements = await Task.Run(() => TextSearchEngine.PrepareReplacements(snapshots, query, plannedMatches, replacement));
      if (_navigationQuery != query || documents.Any(document => !IsCachedDocumentCurrent(document))) return;
      var plan = plannedMatches.Select((match, index) => (match, text: replacements[index])).ToDictionary(item => item.match, item => item.text);
      foreach (var document in documents)
      {
        if (document.Editor.TextEditor.IsReadOnly || document.Matches.Count == 0) continue;
        // One document update and one Undo group for the whole batch.
        using (document.Document.RunUpdate())
        {
          for (int index = document.Matches.Count - 1; index >= 0; index--)
            ReplaceCachedMatch(document, document.Matches[index], plan[document.Matches[index]]);
        }
      }
      var updated = await GetCachedDocumentsAsync(query, allDocuments);
      foreach (var document in updated) document.Editor.MarkerService.SetSearchMarkers(document.Matches, -1);
      EventAggregator.Publish(new SearchEvents.SearchProgressChanged(0, updated.Sum(document => document.Matches.Count)));
      return;
    }
    int active = GetActiveSearchTargetIndex(documents.Select(document => document.Target).ToList());
    if (active < 0 && _selectionOnly) active = 0;
    if (active < 0) return;
    var current = documents[active];
    int caret = current.Editor.TextArea.Caret.Offset;
    TextSearchMatch? selected = null;
    if (ReferenceEquals(current, _navigationDocument) && _navigationIndex >= 0
        && _navigationIndex < current.Matches.Count && caret == current.Matches[_navigationIndex].StartOffset + current.Matches[_navigationIndex].Length)
      selected = current.Matches[_navigationIndex];
    selected ??= current.Matches.FirstOrDefault(match => match.StartOffset <= caret && caret <= match.StartOffset + match.Length)
      ?? current.Matches.FirstOrDefault(match => match.StartOffset >= caret);
    for (int step = 0; selected == null && step < documents.Count; step++)
    {
      active = (active + 1) % documents.Count;
      current = documents[active];
      selected = current.Matches.FirstOrDefault();
    }
    if (selected == null || current.Editor.TextEditor.IsReadOnly) return;
    if (!ReferenceEquals(current.Editor, initialEditor))
    {
      multiEditorControl.controlManager.ShowControl(current.Target.container, current.Target.page);
      current.Target.item.Show(current.Target.container.DockManager);
    }
    string expandedReplacement = (await Task.Run(() => TextSearchEngine.PrepareReplacements(
      new[] { current.Snapshot }, query, new[] { selected }, replacement)))[0];
    if (_navigationQuery != query || !IsCachedDocumentCurrent(current)) return;
    ReplaceCachedMatch(current, selected, expandedReplacement);
    current.Editor.TextArea.ClearSelection();
    current.Editor.TextArea.Caret.Offset = selected.StartOffset + expandedReplacement.Length;
    await NavigateCachedAsync(query, allDocuments, true);
  }

  private static void ReplaceCachedMatch(NavigationDocument document, TextSearchMatch match, string replacement)
  {
    var original = document.Document.GetText(match.StartOffset, match.Length);
    document.Document.Replace(match.StartOffset, match.Length, replacement);
    if (replacement.Length > 0) document.Editor.MarkerService.AddReplacementMarker(match.StartOffset, replacement.Length, original);
  }

  private void GoToCachedMatch(IReadOnlyList<NavigationDocument> documents, NavigationDocument document, int index)
  {
    if (!IsCachedDocumentCurrent(document) || index < 0 || index >= document.Matches.Count) return;
    var match = document.Matches[index];
    if (!ReferenceEquals(_navigationDocument, document) && _navigationDocument != null)
    {
      if (IsCachedDocumentCurrent(_navigationDocument))
        _navigationDocument.Editor.MarkerService.SetSearchMarkers(_navigationDocument.Matches, -1);
      else _navigationDocument.Editor.MarkerService.ClearAllMarkers(preserveReplacements: true);
    }
    var active = GetActiveSearchTargetIndex(documents.Select(item => item.Target).ToList());
    if (active < 0 || !ReferenceEquals(documents[active], document))
    {
      multiEditorControl.controlManager.ShowControl(document.Target.container, document.Target.page);
      document.Target.item.Show(document.Target.container.DockManager);
    }
    document.Editor.MarkerService.SetSearchMarkers(document.Matches, index);
    if (document.Target.item.Content is TranslatorItem translator)
      HighlightTranslatorCommandBySourceLine(translator, match.LineNumber);
    document.Editor.TextArea.Caret.Offset = match.StartOffset + match.Length;
    // Bring into view only when necessary; do not center the line and then scroll a second time.
    document.Editor.TextArea.Caret.BringCaretToView();
    _navigationDocument = document;
    _navigationIndex = index;
    int preceding = 0;
    foreach (var item in documents)
    {
      if (ReferenceEquals(item, document)) break;
      preceding += item.Matches.Count;
    }
    EventAggregator.Publish(new SearchEvents.SearchProgressChanged(preceding + index + 1, documents.Sum(item => item.Matches.Count)));
  }
}
