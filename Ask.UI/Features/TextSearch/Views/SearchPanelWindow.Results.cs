using Ask.Core.Shared.Interfaces.UiInterfaces;
using System.Windows;
using System.Windows.Controls;

namespace Ask.UI.Features.TextSearch.Views;

public partial class SearchPanelWindow
{
  private IReadOnlyList<SearchPanelResult> _results = Array.Empty<SearchPanelResult>();
  private int _resultsPage;
  private bool _updatingResults;
  private const int ResultsPageSize = 5;

  private async Task RunFindAllAsync()
  {
    if (_busy || _closed) return;
    try
    {
      SynchronizeTarget();
      if (string.IsNullOrEmpty(SearchInput.Text)) { ClearSearchResults(); return; }
      _busy = true;
      UpdateReplacementState();
      long version = _targetVersion;
      var query = GetQuery();
      IReadOnlyList<SearchPanelResult> rows = Array.Empty<SearchPanelResult>();
      var session = _session;
      if (session != null)
      {
        if (!await session.SearchAsync(query, TextSearchAction.FindAll, GetDocumentId()) || _closed || !ReferenceEquals(session, _session)) return;
        var documents = _target!.GetSearchDocuments().ToDictionary(document => document.Id);
        var resultMatches = session.Matches;
        rows = new SearchPanelResultCollection(resultMatches.Count,
          resultMatches.Select(match => match.DocumentId).Distinct().Count(), index =>
        {
          var match = resultMatches[index];
          return new SearchPanelResult(documents[match.DocumentId], match, () =>
          {
            if (!ReferenceEquals(session, _session) || !ReferenceEquals(resultMatches, session.Matches)
                || !session.SelectMatch(match)) return false;
            UpdateTargetStatus();
            return true;
          });
        });
        UpdateTargetStatus();
      }
      else if (EditorFindAllAsync != null)
      {
        rows = await EditorFindAllAsync(query, _editorScope == 0);
        if (_closed || version != _targetVersion) { ClearEditorHighlights?.Invoke(); return; }
        SearchStatus.Text = $"0 из {rows.Count}";
      }
      if (_closed || version != _targetVersion || GetQuery() != query)
      {
        if (!_closed) ClearSearchResults();
        else if (session == null) ClearEditorHighlights?.Invoke();
        return;
      }
      _results = rows;
      _resultsPage = 0;
      SetSectionExpanded(ResultsBody, true);
      CollapseResultsButton.IsArrowUp = true;
      int fileCount = rows is SearchPanelResultCollection collection ? collection.DocumentCount : rows.Select(row => row.Document.Id).Distinct().Count();
      ResultsSummary.Text = $"{rows.Count} совпадений в {fileCount} файлах";
      UpdateShellCorners();
      FindAllButton.SetResourceReference(Control.ForegroundProperty, "SearchAccentBrush");
      FindAllButton.SetResourceReference(Control.BorderBrushProperty, "SearchAccentBrush");
      ShowResultsPage();
      SetSectionExpanded(ResultsPanel, true);
      UpdateShellCorners();
    }
    catch (Exception exception) { ClearResultRows(); ShowSearchError(exception); }
    finally { _busy = false; if (!_closed) UpdateReplacementState(); }
  }

  private void ShowResultsPage()
  {
    _updatingResults = true;
    try
    {
      int pageCount = Math.Max(1, (_results.Count + ResultsPageSize - 1) / ResultsPageSize);
      _resultsPage = Math.Clamp(_resultsPage, 0, pageCount - 1);
      int start = _resultsPage * ResultsPageSize;
      ResultsGrid.ItemsSource = Enumerable.Range(start, Math.Min(ResultsPageSize, Math.Max(0, _results.Count - start)))
        .Select(index => _results[index]).ToArray();
      EmptyResults.Visibility = _results.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
      ResultsRange.Text = _results.Count == 0 ? "0 из 0" : $"{_resultsPage * ResultsPageSize + 1}–{Math.Min((_resultsPage + 1) * ResultsPageSize, _results.Count)} из {_results.Count}";
      PreviousResultsPage.IsEnabled = _resultsPage > 0;
      NextResultsPage.IsEnabled = _resultsPage < pageCount - 1;
      ResultsPages.Children.Clear();
      int first = Math.Clamp(_resultsPage - 1, 0, Math.Max(0, pageCount - 4));
      for (int page = first; page < Math.Min(pageCount, first + 4); page++)
      {
        int selectedPage = page;
        var button = new Button { Content = (page + 1).ToString(), Width = 26, Height = 26,
          Style = (Style)FindResource("RoundAction"), FontSize = 11, ToolTip = $"Страница {page + 1}" };
        if (page == _resultsPage)
        {
          button.SetResourceReference(Control.ForegroundProperty, "SearchAccentBrush");
          button.SetResourceReference(Control.BorderBrushProperty, "SearchAccentBrush");
        }
        button.Click += (_, _) => { _resultsPage = selectedPage; ShowResultsPage(); };
        ResultsPages.Children.Add(button);
      }
    }
    finally { _updatingResults = false; }
  }

  private void ClearResultRows()
  {
    _results = Array.Empty<SearchPanelResult>();
    _updatingResults = true;
    ResultsGrid.ItemsSource = null;
    _updatingResults = false;
    SetSectionExpanded(ResultsPanel, false);
    FindAllButton.SetResourceReference(Control.ForegroundProperty, "ForegrounfBrushes");
    FindAllButton.SetResourceReference(Control.BorderBrushProperty, "SearchPanelOutline");
    UpdateShellCorners();
  }

  private void UpdateShellCorners() => PanelShell.CornerRadius = new CornerRadius(
    SettingsPanel.Visibility == Visibility.Visible || ResultsPanel.Visibility == Visibility.Visible ? 20 : 24);

  private void ResultsViewport_SizeChanged(object sender, SizeChangedEventArgs e)
  {
    // Border.ClipToBounds clips a rectangle, not its rounded corners.
    ResultsViewport.Clip = new System.Windows.Media.RectangleGeometry(
      new Rect(0, 0, e.NewSize.Width, e.NewSize.Height), 5, 5);
  }

  private void ResultsGrid_SizeChanged(object sender, SizeChangedEventArgs e)
  {
    if (e.NewSize.Width > 200 && ResultsGrid.Columns.Count == 3)
      ResultsGrid.Columns[2].Width = new DataGridLength(Math.Max(40, e.NewSize.Width - 136 - 54));
  }

  private void ResultsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
  {
    if (_updatingResults || _busy || ResultsGrid.SelectedItem is not SearchPanelResult result) return;
    try
    {
      if (!result.Navigate())
      {
        ClearSearchResults();
        SearchStatus.Text = "Обновите поиск";
      }
      else
      {
        int index = result.ResultIndex >= 0 ? result.ResultIndex
          : Enumerable.Range(0, _results.Count).FirstOrDefault(index => ReferenceEquals(_results[index], result), -1);
        SearchStatus.Text = $"{index + 1} из {_results.Count}";
      }
    }
    catch (Exception exception) { ClearResultRows(); ShowSearchError(exception); }
  }

  private async void FindAll_Click(object sender, RoutedEventArgs e) => await RunFindAllAsync();
  private void PreviousResultsPage_Click(object sender, RoutedEventArgs e) { _resultsPage--; ShowResultsPage(); }
  private void NextResultsPage_Click(object sender, RoutedEventArgs e) { _resultsPage++; ShowResultsPage(); }
  private void CollapseResults_Click(object sender, RoutedEventArgs e)
  {
    bool collapse = IsSectionExpanded(ResultsBody);
    SetSectionExpanded(ResultsBody, !collapse);
    CollapseResultsButton.IsArrowUp = !collapse;
    CollapseResultsButton.ToolTip = collapse ? "Развернуть результаты" : "Свернуть результаты";
  }
}
