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
      Find<ToggleButton>(panel, "RegexOption").IsChecked = true;
      await panel.RunSearchAsync(TextSearchAction.Next);
      Assert.Equal(new TextSearchQuery("cat", true, true, true), lastQuery);
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
      await Task.Delay(350);
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
      Assert.True(Find<ToggleButton>(panel, "RegexOption").IsEnabled);
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

  [Fact]
  public void FindAll_ShowsFiveRowsPerPageAndSupportsBothSettingsLayouts() => RunInSta(async () =>
  {
    var owner = new Window();
    new System.Windows.Interop.WindowInteropHelper(owner).EnsureHandle();
    var panel = new SearchPanelWindow(owner, new Grid()) { Opacity = 0, ShowActivated = false };
    try
    {
      int navigated = -1;
      bool searchedAll = false;
      panel.EditorFindAllAsync = (query, all) =>
      {
        Assert.Equal("cat", query.Text);
        searchedAll = all;
        IReadOnlyList<SearchPanelResult> rows = Enumerable.Range(0, 18).Select(index =>
        {
          int selected = index;
          return new SearchPanelResult(new SearchDocument((index % 4).ToString(), $"file{index % 4}.txt", "prefix cat suffix"),
            new TextSearchMatch((index % 4).ToString(), 7, 3, index + 1, 7), () => { navigated = selected; return true; });
        }).ToArray();
        return Task.FromResult(rows);
      };
      panel.SetSearchText("cat");
      panel.ShowPanel();
      await panel.RunSearchAsync(TextSearchAction.FindAll);
      await Task.Delay(350);
      panel.UpdateLayout();
      Assert.True(searchedAll);
      Assert.Equal(5, Find<DataGrid>(panel, "ResultsGrid").Items.Count);
      Assert.Equal("1–5 из 18", Find<TextBlock>(panel, "ResultsRange").Text);
      Assert.Contains("18", Find<TextBlock>(panel, "ResultsSummary").Text);
      Assert.Contains("4", Find<TextBlock>(panel, "ResultsSummary").Text);
      Assert.True(Find<DataGrid>(panel, "ResultsGrid").Columns[2].ActualWidth > 200);
      var resultsGrid = Find<DataGrid>(panel, "ResultsGrid");
      Assert.Equal(DataGridGridLinesVisibility.None, resultsGrid.GridLinesVisibility);
      Assert.InRange(Math.Abs(resultsGrid.Columns.Sum(column => column.ActualWidth) - resultsGrid.ActualWidth), 0, 1);
      var viewport = Find<Grid>(panel, "ResultsViewport");
      var roundedClip = Assert.IsType<System.Windows.Media.RectangleGeometry>(viewport.Clip);
      Assert.Equal(5, roundedClip.RadiusX);
      Assert.Equal(viewport.ActualWidth, roundedClip.Rect.Width);
      Assert.Equal(viewport.ActualHeight, roundedClip.Rect.Height);
      double resultsHeight = panel.ActualHeight;
      Assert.True(resultsHeight > 200);
      Click(panel, "NextResultsPage");
      Assert.Equal("6–10 из 18", Find<TextBlock>(panel, "ResultsRange").Text);
      Find<DataGrid>(panel, "ResultsGrid").SelectedIndex = 1;
      Assert.Equal(6, navigated);
      var pageFour = Find<StackPanel>(panel, "ResultsPages").Children.OfType<Button>().Single(button => Equals(button.Content, "4"));
      pageFour.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
      Assert.Equal(3, Find<DataGrid>(panel, "ResultsGrid").Items.Count);
      Assert.Equal("16–18 из 18", Find<TextBlock>(panel, "ResultsRange").Text);
      Assert.False(Find<Button>(panel, "NextResultsPage").IsEnabled);
      Click(panel, "SettingsButton");
      await Task.Delay(350);
      panel.UpdateLayout();
      Assert.True(panel.ActualHeight > resultsHeight);
      Assert.Equal(Visibility.Visible, Find<System.Windows.Controls.Border>(panel, "ResultsPanel").Visibility);
      Click(panel, "CollapseResultsButton");
      await Task.Delay(350);
      Assert.Equal(Visibility.Collapsed, Find<StackPanel>(panel, "ResultsBody").Visibility);
      Click(panel, "CollapseResultsButton");
      await Task.Delay(350);
      Click(panel, "SettingsButton");
      await Task.Delay(350);
      panel.UpdateLayout();
      Assert.Equal(resultsHeight, panel.ActualHeight);
      panel.SetSearchText("changed");
      await Task.Delay(350);
      await Task.Delay(350);
      Assert.Equal(Visibility.Collapsed, Find<System.Windows.Controls.Border>(panel, "ResultsPanel").Visibility);
    }
    finally { panel.Close(); owner.Close(); }
  });

  [Theory]
  [InlineData(false)]
  [InlineData(true)]
  public void FindAll_ComponentRowsSelectExactMatchAndRejectStaleSnapshots(bool refreshSessionFirst) => RunInSta(async () =>
  {
    var owner = new Window();
    new System.Windows.Interop.WindowInteropHelper(owner).EnsureHandle();
    var target = new EditableTarget();
    var panel = new SearchPanelWindow(owner, new Grid()) { TargetProvider = () => target };
    try
    {
      panel.SetSearchText("cat");
      Find<ToggleButton>(panel, "WholeWordOption").IsChecked = true;
      await panel.RunSearchAsync(TextSearchAction.FindAll);
      await Task.Delay(350);
      var grid = Find<DataGrid>(panel, "ResultsGrid");
      Assert.Equal(4, grid.Items.Count);
      grid.SelectedIndex = 2;
      Assert.Equal(16, target.Current!.StartOffset);
      Assert.Equal("3 из 4", Find<TextBlock>(panel, "SearchStatus").Text);
      await panel.RunSearchAsync(TextSearchAction.Next);
      Assert.Equal("target", target.Current!.DocumentId);
      Assert.Equal("4 из 4", Find<TextBlock>(panel, "SearchStatus").Text);
      target.ReplaceSearchResult(target.Current, "dog");
      if (refreshSessionFirst) await panel.RunSearchAsync(TextSearchAction.Next);
      grid.SelectedIndex = 0;
      await Task.Delay(350);
      Assert.Equal(Visibility.Collapsed, Find<System.Windows.Controls.Border>(panel, "ResultsPanel").Visibility);
      Assert.Equal("Обновите поиск", Find<TextBlock>(panel, "SearchStatus").Text);
    }
    finally { panel.Close(); owner.Close(); }
  });

  [Fact]
  public void FindAll_EmptyAndClosedRequestsDoNotLeaveRowsOrHighlights() => RunInSta(async () =>
  {
    var owner = new Window();
    new System.Windows.Interop.WindowInteropHelper(owner).EnsureHandle();
    var pending = new TaskCompletionSource<IReadOnlyList<SearchPanelResult>>(TaskCreationOptions.RunContinuationsAsynchronously);
    int clears = 0;
    var panel = new SearchPanelWindow(owner, new Grid())
    {
      EditorFindAllAsync = (_, _) => Task.FromResult<IReadOnlyList<SearchPanelResult>>(Array.Empty<SearchPanelResult>()),
      ClearEditorHighlights = () => clears++
    };
    try
    {
      panel.SetSearchText("cat");
      await panel.RunSearchAsync(TextSearchAction.FindAll);
      await Task.Delay(350);
      Assert.Equal(Visibility.Visible, Find<TextBlock>(panel, "EmptyResults").Visibility);
      Assert.Equal("0 из 0", Find<TextBlock>(panel, "ResultsRange").Text);
      panel.EditorFindAllAsync = (_, _) => pending.Task;
      var request = panel.RunSearchAsync(TextSearchAction.FindAll);
      panel.Close();
      pending.SetResult(Array.Empty<SearchPanelResult>());
      await request;
      Assert.True(clears >= 2);
    }
    finally { panel.Close(); owner.Close(); }
  });

  [Fact]
  public void EditorNavigation_ReusesUnchangedFilesAndWrapsAcrossDuplicateNames() => RunInSta(async () =>
  {
    var workspace = new global::UI.Components.MultiEditorControl();
    var manager = (global::UI.Components.MultiEditorMethods.FileManager)workspace.GetType()
      .GetField("fileManager", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(workspace)!;
    var search = (global::UI.Components.MultiEditorMethods.TextSearchManager)workspace.GetType()
      .GetField("textSearchManager", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(workspace)!;
    var container = new global::UI.Controls.TextEditorControl.TextEditorContainer();
    var page = new global::UI.Components.Invoke.OpenFileButton { Text = "TextEditor", IsActive = true };
    manager.EditorWorkspaceModel.OpenPages.Add(page);
    manager.EditorWorkspaceModel.UserControls.Add(container);
    workspace.ContentPanel.Children.Add(container);
    var first = new global::UI.Controls.TextEditorControl.TextEditorUI { Text = "cat one cat" };
    var second = new global::UI.Controls.TextEditorControl.TextEditorUI { Text = "cat two" };
    var one = new global::UI.Windows.WpfDocking.Windows.Docking.DockItem { Title = "same.txt", Content = first };
    var two = new global::UI.Windows.WpfDocking.Windows.Docking.DockItem { Title = "same.txt", Content = second };
    var window = new Window { Content = workspace, Width = 700, Height = 400, Opacity = 0, ShowInTaskbar = false, ShowActivated = false };
    try
    {
      window.Show();
      window.UpdateLayout();
      one.Show(container.DockManager, global::UI.Windows.WpfDocking.Windows.Docking.DockPosition.Document);
      two.Show(container.DockManager, global::UI.Windows.WpfDocking.Windows.Docking.DockPosition.Document);
      one.Show(container.DockManager);
      var rows = await search.FindAllForPanelAsync(new TextSearchQuery("cat"), true);
      Assert.Equal(3, rows.Count);
      Assert.Equal(2, rows.Select(row => row.Document.Id).Distinct().Count());
      var repeated = await search.FindAllForPanelAsync(new TextSearchQuery("cat"), true);
      Assert.Same(rows, repeated);
      Assert.True(rows[0].Navigate());
      Assert.Equal(3, first.TextArea.Caret.Offset);
      await search.SearchData("cat", false, false, 1, "FindNext");
      Assert.Equal(11, first.TextArea.Caret.Offset);
      await search.SearchData("cat", false, false, 1, "FindNext");
      Assert.True(two.IsActiveItem);
      Assert.Equal(3, second.TextArea.Caret.Offset);
      await search.SearchData("cat", false, false, 1, "FindNext");
      Assert.True(one.IsActiveItem);
      Assert.Equal(3, first.TextArea.Caret.Offset);
      await search.SearchData("cat", false, false, 1, "FindPrevious");
      Assert.True(two.IsActiveItem);
      var oldFirstSnapshot = rows[0].Document;
      second.TextEditor.Document.Insert(0, "cat ");
      Assert.False(rows[2].Navigate());
      var updated = await search.FindAllForPanelAsync(new TextSearchQuery("cat"), true);
      Assert.Equal(4, updated.Count);
      Assert.Same(oldFirstSnapshot, updated[0].Document);
      Assert.True(updated[3].Navigate());
      Assert.Equal(7, second.TextArea.Caret.Offset);
      await workspace.ReplaceWordData("dog", "cat", false, false, 1, "FindAll");
      Assert.Equal("dog one dog", first.Text);
      Assert.Equal("dog dog two", second.Text);
      first.TextEditor.Document.UndoStack.Undo();
      Assert.Equal("cat one cat", first.Text);
      one.Show(container.DockManager);
      first.Text = "cat cat cat";
      second.Text = "cat";
      first.TextEditor.Select(4, 3);
      Assert.True(search.CaptureSearchSelection());
      search.SetSelectionSearch(true);
      var selectedRows = await search.FindAllForPanelAsync(new TextSearchQuery("cat"), true);
      Assert.Single(selectedRows);
      Assert.Equal(4, selectedRows[0].Match.StartOffset);
      Assert.True(selectedRows[0].Navigate());
      await search.SearchData("cat", false, false, 1, "FindNext");
      Assert.Equal(7, first.TextArea.Caret.Offset);
      first.TextEditor.Document.Insert(0, "cat ");
      selectedRows = await search.FindAllForPanelAsync(new TextSearchQuery("cat"), true);
      Assert.Single(selectedRows);
      Assert.Equal(8, selectedRows[0].Match.StartOffset);
      await search.ReplaceCachedAsync(new TextSearchQuery("cat"), "kitten", true, true);
      Assert.Equal("cat cat kitten cat", first.Text);
      Assert.Equal("cat", second.Text);
      var replacedSelection = await search.FindAllForPanelAsync(new TextSearchQuery("kitten"), true);
      Assert.Single(replacedSelection);
      Assert.Equal(8, replacedSelection[0].Match.StartOffset);
      await search.ReplaceCachedAsync(new TextSearchQuery("kit(t)en", UseRegex: true), "$1", true, true);
      Assert.Equal("cat cat t cat", first.Text);
      search.SetSelectionSearch(false);
      Assert.Equal(4, (await search.FindAllForPanelAsync(new TextSearchQuery("cat"), true)).Count);
      first.TextEditor.Select(1, 2);
      Assert.True(search.CaptureSearchSelection());
      search.SetSelectionSearch(true);
      Assert.Empty(await search.FindAllForPanelAsync(new TextSearchQuery("cat"), true));
      search.ReleaseSearchSelection();
      first.Text = "X12 X34";
      second.Text = "X56";
      first.TextArea.Caret.Offset = 0;
      await workspace.SearchData(@"X\d+", false, false, 1, "FindNext", true);
      Assert.Equal(3, first.TextArea.Caret.Offset);
      Assert.Equal(3, (await search.FindAllForPanelAsync(new TextSearchQuery(@"X\d+", UseRegex: true), true)).Count);
      await workspace.ReplaceWordData("$1", @"X(\d+)", false, false, 1, "FindAll", true);
      Assert.Equal("12 34", first.Text);
      Assert.Equal("56", second.Text);
      var zeroLength = await search.FindAllForPanelAsync(new TextSearchQuery("^|$", UseRegex: true), true);
      Assert.Equal(4, zeroLength.Count);
      Assert.True(zeroLength[0].Navigate());
      await search.SearchData("^|$", false, false, 1, "FindNext", true);
      Assert.Equal(5, first.TextArea.Caret.Offset);
      search.OnSearchWindowClosing();
    }
    finally { window.Close(); }
  });

  [Fact]
  public void ComparisonNavigation_UpdatesOnlyOldAndNewMatchLines() => RunInSta(() =>
  {
    var searchIndex = new Ask.UI.Features.FileComparison.Views.ComparisonSearchIndex();
    var source = Enumerable.Range(1, 3).Select(index => new Ask.UI.Features.FileComparison.Views.DiffLineViewModel(
      Ask.UI.Features.FileComparison.Views.DiffLineKind.Unchanged,
      Ask.UI.Features.FileComparison.Views.DiffPaneSide.Source, index, index, index < 3 ? "cat" : "none")).ToArray();
    var target = Enumerable.Range(1, 3).Select(index => new Ask.UI.Features.FileComparison.Views.DiffLineViewModel(
      Ask.UI.Features.FileComparison.Views.DiffLineKind.Unchanged,
      Ask.UI.Features.FileComparison.Views.DiffPaneSide.Target, index, index, index < 3 ? "cat" : "none")).ToArray();
    var comparison = new Ask.UI.Features.FileComparison.Views.FileComparisonResult(
      new("same.txt", "left.txt", "cat\ncat\nnone"), new("same.txt", "right.txt", "cat\ncat\nnone"), source, target);
    var matches = Ask.Core.Services.TextSearch.TextSearchEngine.Find(new[] {
      new SearchDocument("source", "source", comparison.SourceFile.TextContent),
      new SearchDocument("target", "target", comparison.TargetFile.TextContent)
    }, new TextSearchQuery("cat"));
    searchIndex.Update(comparison, matches, matches[0]);
    var unchanged = target[0].SearchRanges;
    var emptyLine = source[2].SearchRanges;
    Assert.True(source[0].SearchRanges[0].IsCurrent);
    searchIndex.Update(comparison, matches, matches[1]);
    Assert.False(source[0].SearchRanges[0].IsCurrent);
    Assert.True(source[1].SearchRanges[0].IsCurrent);
    Assert.Same(unchanged, target[0].SearchRanges);
    Assert.Same(emptyLine, source[2].SearchRanges);
    searchIndex.Update(comparison, Array.Empty<TextSearchMatch>(), null);
    Assert.All(source.Concat(target), line => Assert.Empty(line.SearchRanges));
    return Task.CompletedTask;
  });

  [Fact]
  public void RegexError_IsShownWithoutBreakingThePanel() => RunInSta(async () =>
  {
    var owner = new Window();
    new System.Windows.Interop.WindowInteropHelper(owner).EnsureHandle();
    var panel = new SearchPanelWindow(owner, new Grid());
    var target = new ReadOnlyTarget();
    panel.TargetProvider = () => target;
    try
    {
      Find<ToggleButton>(panel, "RegexOption").IsChecked = true;
      panel.SetSearchText("[");
      await panel.RunSearchAsync(TextSearchAction.Next);
      Assert.Equal("0 из 0", Find<TextBlock>(panel, "SearchStatus").Text);
      Assert.Null(Find<TextBlock>(panel, "SearchStatus").ToolTip);
      Assert.True(Find<ToggleButton>(panel, "RegexOption").IsEnabled);
      panel.SetSearchText("c.t");
      await panel.RunSearchAsync(TextSearchAction.Next);
      Assert.Equal("1 из 1", Find<TextBlock>(panel, "SearchStatus").Text);
    }
    finally { panel.Close(); owner.Close(); }
  });

  [Fact]
  public void SelectionOption_UpdatesEditorModeAndLocksDocumentScope() => RunInSta(() =>
  {
    var owner = new Window();
    new System.Windows.Interop.WindowInteropHelper(owner).EnsureHandle();
    bool selectionOnly = false;
    var panel = new SearchPanelWindow(owner, new Grid())
    {
      CanSearchEditorSelection = () => true,
      SetEditorSelectionOnly = enabled => selectionOnly = enabled
    };
    try
    {
      panel.SynchronizeTarget();
      var option = Find<CheckBox>(panel, "SelectionOnlyOption");
      Assert.True(option.IsEnabled);
      option.IsChecked = true;
      Assert.True(selectionOnly);
      Assert.False(Find<ComboBox>(panel, "SearchScope").IsEnabled);
      Click(panel, "ResetSettingsButton");
      Assert.False(selectionOnly);
      Assert.True(Find<ComboBox>(panel, "SearchScope").IsEnabled);
    }
    finally { panel.Close(); owner.Close(); }
    return Task.CompletedTask;
  });

  [Fact]
  public void AnimatedClose_KeepsInputShieldAndLeavesOwnerOpen() => RunInSta(async () =>
  {
    var owner = new Window { ShowActivated = false, Opacity = 0 };
    new System.Windows.Interop.WindowInteropHelper(owner).EnsureHandle();
    var panel = new SearchPanelWindow(owner, new Grid()) { ShowActivated = false, Opacity = 0 };
    bool closed = false;
    panel.Closed += (_, _) => closed = true;
    try
    {
      owner.Show();
      panel.ShowPanel();
      panel.CloseAnimated();
      if (SystemParameters.ClientAreaAnimation)
      {
        Assert.False(closed);
        Assert.True(Find<Grid>(panel, "MotionRoot").IsHitTestVisible);
        var mouse = new System.Windows.Input.MouseButtonEventArgs(System.Windows.Input.Mouse.PrimaryDevice,
          Environment.TickCount, System.Windows.Input.MouseButton.Left)
        { RoutedEvent = System.Windows.Input.Mouse.PreviewMouseDownEvent };
        panel.RaiseEvent(mouse);
        Assert.True(mouse.Handled);
      }
      await WaitUntil(() => closed);
      Assert.True(owner.IsVisible);
      Assert.Equal(WindowState.Normal, owner.WindowState);
    }
    finally { if (!closed) panel.Close(); owner.Close(); }
  });

  private static T Find<T>(SearchPanelWindow panel, string name) where T : class => (T)panel.FindName(name);
  private static void Click(SearchPanelWindow panel, string name)
  {
    var control = Find<UIElement>(panel, name);
    control.RaiseEvent(new RoutedEventArgs(control is Ask.UI.Shared.Components.Icons.ArrowButton
      ? Ask.UI.Shared.Components.Icons.ArrowButton.ClickEvent : ButtonBase.ClickEvent));
  }

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
