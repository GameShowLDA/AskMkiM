using Ask.Core.Services.EventCore.Events;
using Ask.Core.Services.EventCore.Services;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.UI.Features.TextSearch.Views;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;

namespace Ask.UI.UnitTests.Services.TextSearch;

[CollectionDefinition("Search panel", DisableParallelization = true)]
public sealed class SearchPanelCollection { }

[Collection("Search panel")]
public sealed class SearchPanelIntegrationTests
{
  [Fact]
  public void EditorCallbacks_ReceiveOptionsScopesAndReplacementActions() => RunInSta(async () =>
  {
    var owner = new Window();
    new System.Windows.Interop.WindowInteropHelper(owner).EnsureHandle();
    var panel = new SearchPanelWindow(owner, new Grid());
    try
    {
      int replacementHistoryClears = 0;
      panel.ClearReplacementHighlights = () => replacementHistoryClears++;
      TextSearchQuery? lastQuery = null;
      bool allDocuments = false;
      var actions = new List<TextSearchAction>();
      var replacements = new List<(string Text, bool All)>();
      panel.CanReplaceEditor = () => true;
      panel.EditorSearchAsync = (query, all, action) =>
      {
        lastQuery = query; allDocuments = all; actions.Add(action);
        EventAggregator.Publish(new SearchEvents.SearchProgressChanged(2, 5));
        return Task.CompletedTask;
      };
      panel.EditorReplaceAsync = (query, text, all, replaceAll) =>
      {
        replacements.Add((text, replaceAll));
        allDocuments = all;
        return Task.CompletedTask;
      };
      panel.SetSearchText("cat");
      Find<ToggleButton>(panel, "MatchCaseOption").IsChecked = true;
      Find<ToggleButton>(panel, "WholeWordOption").IsChecked = true;
      await panel.RunSearchAsync(TextSearchAction.Next);
      Assert.Equal(new TextSearchQuery("cat", true, true), lastQuery);
      Assert.True(allDocuments);
      Assert.Equal("2 из 5", Find<TextBlock>(panel, "SearchStatus").Text);
      Find<ComboBox>(panel, "SearchScope").SelectedIndex = 1;
      await panel.RunSearchAsync(TextSearchAction.Previous);
      Assert.False(allDocuments);
      Assert.Equal(new[] { TextSearchAction.Next, TextSearchAction.Previous }, actions);
      Find<TextBox>(panel, "ReplacementInput").Text = "dog";
      Click(panel, "ReplaceButton");
      Click(panel, "ReplaceAllButton");
      Assert.Equal(new[] { ("dog", false), ("dog", true) }, replacements);
      await panel.RunSearchAsync(TextSearchAction.FindAll);
      Assert.Equal(2, actions.Count);
      Assert.Equal(0, replacementHistoryClears);
      panel.Close();
      Assert.Equal(1, replacementHistoryClears);
    }
    finally { panel.Close(); owner.Close(); }
  });

  [Fact]
  public void ComponentSession_NavigatesReplacesRestrictsScopeAndClearsOnClose() => RunInSta(async () =>
  {
    var owner = new Window();
    new System.Windows.Interop.WindowInteropHelper(owner).EnsureHandle();
    var target = new EditableTarget();
    var panel = new SearchPanelWindow(owner, new Grid()) { TargetProvider = () => target };
    try
    {
      panel.SetSearchText("cat");
      Find<ToggleButton>(panel, "WholeWordOption").IsChecked = true;
      await panel.RunSearchAsync(TextSearchAction.Next);
      Assert.Equal(4, target.Highlights.Count);
      Assert.Equal(0, target.Current!.StartOffset);
      await panel.RunSearchAsync(TextSearchAction.Next);
      Assert.Equal(4, target.Current!.StartOffset);
      Find<TextBox>(panel, "ReplacementInput").Text = "dog";
      Click(panel, "ReplaceButton");
      await WaitUntil(() => Find<Button>(panel, "NextButton").IsEnabled);
      Assert.Equal("CAT dog scatter cat", target.Source);
      Find<ComboBox>(panel, "SearchScope").SelectedIndex = 2;
      Assert.Equal("target", target.SelectedSearchDocumentId);
      await panel.RunSearchAsync(TextSearchAction.Next);
      Assert.Single(target.Highlights);
      Assert.Equal("target", target.Current!.DocumentId);
      Click(panel, "ReplaceAllButton");
      await WaitUntil(() => Find<Button>(panel, "NextButton").IsEnabled);
      Assert.Equal("dog", target.Destination);
      Assert.Equal("CAT dog scatter cat", target.Source);
      Assert.Equal("0 из 0", Find<TextBlock>(panel, "SearchStatus").Text);
      Click(panel, "ResetSettingsButton");
      Assert.Null(target.SelectedSearchDocumentId);
      Assert.Empty(Find<TextBox>(panel, "ReplacementInput").Text);
      Assert.False(Find<ToggleButton>(panel, "WholeWordOption").IsChecked);
      await panel.RunSearchAsync(TextSearchAction.Next);
      Assert.NotEmpty(target.Highlights);
    }
    finally { panel.Close(); owner.Close(); }
    Assert.Empty(target.Highlights);
    Assert.Null(target.Current);
  });

  [Fact]
  public void ReadOnlyTarget_DisablesReplacementAndRestoresEditorScope() => RunInSta(async () =>
  {
    var owner = new Window();
    new System.Windows.Interop.WindowInteropHelper(owner).EnsureHandle();
    ITextSearchTarget? current = null;
    var panel = new SearchPanelWindow(owner, new Grid())
    {
      TargetProvider = () => current,
      CanReplaceEditor = () => true
    };
    try
    {
      Find<ComboBox>(panel, "SearchScope").SelectedIndex = 1;
      current = new ReadOnlyTarget();
      panel.SynchronizeTarget();
      Assert.False(Find<Grid>(panel, "ReplacementRow").IsEnabled);
      panel.SetSearchText("cat");
      await panel.RunSearchAsync(TextSearchAction.Next);
      Assert.Equal("1 из 1", Find<TextBlock>(panel, "SearchStatus").Text);
      current = null;
      panel.SynchronizeTarget();
      Assert.True(Find<Grid>(panel, "ReplacementRow").IsEnabled);
      Assert.Equal(1, Find<ComboBox>(panel, "SearchScope").SelectedIndex);
      Assert.False(Find<ToggleButton>(panel, "RegexOption").IsEnabled);
      Assert.False(Find<CheckBox>(panel, "SelectionOnlyOption").IsEnabled);
    }
    finally { panel.Close(); owner.Close(); }
  });

  [Fact]
  public void ClosedPanel_UnsubscribesProgressAndClearsLateLegacyResults() => RunInSta(async () =>
  {
    var owner = new Window();
    new System.Windows.Interop.WindowInteropHelper(owner).EnsureHandle();
    var pending = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    int clears = 0;
    var panel = new SearchPanelWindow(owner, new Grid())
    {
      EditorSearchAsync = (_, _, _) => pending.Task,
      ClearEditorHighlights = () => clears++
    };
    panel.SetSearchText("cat");
    var operation = panel.RunSearchAsync(TextSearchAction.Next);
    panel.Close();
    EventAggregator.Publish(new SearchEvents.SearchProgressChanged(7, 8));
    Assert.Equal("0 из 0", Find<TextBlock>(panel, "SearchStatus").Text);
    pending.SetResult();
    await operation;
    Assert.True(clears >= 2);
    owner.Close();
  });

  private static T Find<T>(SearchPanelWindow panel, string name) where T : class => (T)panel.FindName(name);
  private static void Click(SearchPanelWindow panel, string name) => Find<Button>(panel, name)
    .RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));

  private static async Task WaitUntil(Func<bool> condition)
  {
    var timer = Stopwatch.StartNew();
    while (!condition())
    {
      Assert.True(timer.Elapsed < TimeSpan.FromSeconds(10), "Search operation timed out");
      await Task.Delay(5);
    }
  }

  private static void RunInSta(Func<Task> action)
  {
    Exception? error = null;
    var thread = new Thread(() =>
    {
      var dispatcher = Dispatcher.CurrentDispatcher;
      SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(dispatcher));
      dispatcher.BeginInvoke(new Action(async () =>
      {
        try { await action(); }
        catch (Exception exception) { error = exception; }
        finally { dispatcher.BeginInvokeShutdown(DispatcherPriority.Background); }
      }));
      Dispatcher.Run();
    }) { IsBackground = true };
    thread.SetApartmentState(ApartmentState.STA);
    thread.Start();
    Assert.True(thread.Join(TimeSpan.FromSeconds(30)), "WPF integration test timed out");
    if (error != null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error).Throw();
  }

  private sealed class ReadOnlyTarget : ITextSearchTarget
  {
    public IReadOnlyList<SearchDocument> GetSearchDocuments() => new[] { new SearchDocument("id", "Read only", "cat") };
    public void HighlightSearchResults(IReadOnlyList<TextSearchMatch> matches, TextSearchMatch? current) { }
    public void NavigateToSearchResult(TextSearchMatch match) { }
  }

  private sealed class EditableTarget : IReplaceTextSearchTarget, ITextSearchScopeProvider
  {
    public string Source { get; private set; } = "CAT cat scatter cat";
    public string Destination { get; private set; } = "cat";
    public string AllDocumentsTitle => "Обе панели";
    public string? SelectedSearchDocumentId { get; set; }
    public IReadOnlyList<TextSearchMatch> Highlights { get; private set; } = Array.Empty<TextSearchMatch>();
    public TextSearchMatch? Current { get; private set; }
    public IReadOnlyList<SearchDocument> GetSearchDocuments() => new[]
    {
      new SearchDocument("source", "Левая", Source), new SearchDocument("target", "Правая", Destination)
    };
    public void HighlightSearchResults(IReadOnlyList<TextSearchMatch> matches, TextSearchMatch? current)
    { Highlights = matches; Current = current; }
    public void NavigateToSearchResult(TextSearchMatch match) { }
    public void ReplaceSearchResult(TextSearchMatch match, string replacement)
    {
      if (match.DocumentId == "source") Source = Source.Remove(match.StartOffset, match.Length).Insert(match.StartOffset, replacement);
      else Destination = Destination.Remove(match.StartOffset, match.Length).Insert(match.StartOffset, replacement);
    }
  }
}
