using Ask.Core.Services.EventCore.Adapters;
using Ask.Core.Services.EventCore.Events;
using Ask.Core.Services.EventCore.Services;
using Ask.Core.Services.TextSearch;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.UI.Features.Notifications.Models;
using Ask.UI.Infrastructure.UI.Overlay.Notifications.Runtime;
using System.Windows;
using System.Windows.Controls;
using static Ask.LogLib.LoggerUtility;

namespace Ask.UI.Features.TextSearch.Views;

public partial class SearchPanelWindow
{
  private ITextSearchTarget? _target;
  private TextSearchSession? _session;
  private bool _updatingScopes;
  private bool _busy;
  private bool _closed;
  private int _editorScope;
  private long _targetVersion;

  public Func<ITextSearchTarget?>? TargetProvider { get; set; }
  public Func<bool>? CanReplaceEditor { get; set; }
  public Func<bool>? CanSearchEditorSelection { get; set; }
  public Action<bool>? SetEditorSelectionOnly { get; set; }
  public Func<TextSearchQuery, bool, TextSearchAction, Task>? EditorSearchAsync { get; set; }
  public Func<TextSearchQuery, string, bool, bool, Task>? EditorReplaceAsync { get; set; }
  public Func<TextSearchQuery, bool, Task<IReadOnlyList<SearchPanelResult>>>? EditorFindAllAsync { get; set; }
  public Action? ClearEditorHighlights { get; set; }
  public Action? ClearReplacementHighlights { get; set; }

  private void InitializeSearch()
  {
    EventAggregator.Subscribe<SearchEvents.SearchProgressChanged>(OnEditorProgress);
    EventAggregator.Subscribe<SearchEvents.CloseSearchWindow>(OnCloseRequested);
    SearchScope.SelectionChanged += Scope_SelectionChanged;
    SearchInput.TextChanged += (_, _) => ClearSearchResults();
    MatchCaseOption.Click += (_, _) => ClearSearchResults();
    WholeWordOption.Click += (_, _) => ClearSearchResults();
    RegexOption.Click += (_, _) => ClearSearchResults();
    SelectionOnlyOption.Checked += SelectionOnly_Changed;
    SelectionOnlyOption.Unchecked += SelectionOnly_Changed;
  }

  public void SetSearchText(string? text)
  {
    if (!string.IsNullOrEmpty(text)) SearchInput.Text = text;
  }

  public void SynchronizeTarget()
  {
    if (_closed) return;
    var target = TargetProvider?.Invoke();
    bool changed = !ReferenceEquals(target, _target);
    if (changed)
    {
      _targetVersion++;
      _session?.Dispose();
      if (_target == null) ClearEditorHighlights?.Invoke();
      _target = target;
      _session = target == null ? null : new TextSearchSession(target);
      ResetStatus();
    }
    UpdateScopes(changed);
    if (_target != null && SelectionOnlyOption.IsChecked == true) SelectionOnlyOption.IsChecked = false;
    UpdateReplacementState();
  }

  private void SelectionOnly_Changed(object sender, RoutedEventArgs e)
  {
    if (_closed) return;
    SetEditorSelectionOnly?.Invoke(SelectionOnlyOption.IsChecked == true && _target == null);
    _targetVersion++;
    ClearSearchResults();
    UpdateReplacementState();
  }

  private void UpdateScopes(bool targetChanged)
  {
    _updatingScopes = true;
    try
    {
      if (_target == null)
      {
        if (!targetChanged) return;
        SearchScope.Items.Clear();
        SearchScope.Items.Add(new ComboBoxItem { Content = "Все открытые документы" });
        SearchScope.Items.Add(new ComboBoxItem { Content = "Текущий документ" });
        SearchScope.SelectedIndex = _editorScope;
        return;
      }
      var scope = _target as ITextSearchScopeProvider;
      var selectedId = scope != null ? scope.SelectedSearchDocumentId
        : (targetChanged ? null : (SearchScope.SelectedItem as ComboBoxItem)?.Tag as string);
      var areas = new[] { new SearchDocument("", scope?.AllDocumentsTitle ?? "Все области компонента", "") }
        .Concat(_target.GetSearchDocuments()).ToArray();
      var current = SearchScope.Items.Cast<ComboBoxItem>().ToArray();
      if (!targetChanged && current.Length == areas.Length && current.Select((item, i) =>
        Equals(item.Tag, areas[i].Id) && Equals(item.Content, areas[i].Title)).All(value => value)) return;
      SearchScope.Items.Clear();
      foreach (var area in areas)
        SearchScope.Items.Add(new ComboBoxItem { Content = area.Title, Tag = area.Id });
      SearchScope.SelectedIndex = Math.Max(0, Array.FindIndex(areas, area => area.Id == selectedId));
    }
    finally { _updatingScopes = false; }
  }

  private void Scope_SelectionChanged(object sender, SelectionChangedEventArgs e)
  {
    if (_updatingScopes || _closed) return;
    if (_target == null)
    {
      _editorScope = SearchScope.SelectedIndex;
      ClearEditorHighlights?.Invoke();
    }
    else
    {
      if (_target is ITextSearchScopeProvider scope) scope.SelectedSearchDocumentId = GetDocumentId();
      _session?.Dispose();
      _session = new TextSearchSession(_target);
      _targetVersion++;
    }
    ResetStatus();
  }

  private string? GetDocumentId() => (SearchScope.SelectedItem as ComboBoxItem)?.Tag is string id && id.Length > 0 ? id : null;
  private TextSearchQuery GetQuery() => new(SearchInput.Text, MatchCaseOption.IsChecked == true, WholeWordOption.IsChecked == true, RegexOption.IsChecked == true);

  private void UpdateReplacementState()
  {
    bool allowed = _target != null ? _target is IReplaceTextSearchTarget : CanReplaceEditor?.Invoke() == true;
    ReplacementRow.IsEnabled = allowed && !_busy;
    ReplacementRow.ToolTip = allowed ? null : "Замена недоступна для текущего документа";
    ToolTipService.SetShowOnDisabled(ReplacementRow, true);
    PreviousButton.IsEnabled = NextButton.IsEnabled = FindAllButton.IsEnabled = !_busy;
    ResultsGrid.IsEnabled = !_busy;
    SearchInput.IsReadOnly = _busy;
    MatchCaseOption.IsEnabled = WholeWordOption.IsEnabled = RegexOption.IsEnabled = !_busy;
    SearchScope.IsEnabled = !_busy && SelectionOnlyOption.IsChecked != true;
    SelectionOnlyOption.IsEnabled = !_busy && _target == null && CanSearchEditorSelection?.Invoke() == true;
    SelectionOnlyOption.Opacity = SelectionOnlyOption.IsEnabled ? 1 : 0.4;
    SelectionOnlyOption.ToolTip = _target != null ? "Поиск в выделении доступен в текстовом редакторе"
      : CanSearchEditorSelection?.Invoke() == true ? "Искать только в тексте, выделенном при открытии поиска"
      : "Выделите текст перед открытием поиска (обычное, не прямоугольное выделение)";
    ResetSettingsButton.IsEnabled = !_busy;
  }

  public async Task RunSearchAsync(TextSearchAction action)
  {
    if (action == TextSearchAction.FindAll) { await RunFindAllAsync(); return; }
    if (_busy || _closed) return;
    try
    {
      SynchronizeTarget();
      if (string.IsNullOrEmpty(SearchInput.Text))
      {
        ClearSearchResults();
        return;
      }
      _busy = true;
      UpdateReplacementState();
      long version = _targetVersion;
      var query = GetQuery();
      var session = _session;
      if (session != null)
      {
        if (await session.SearchAsync(query, action, GetDocumentId()) && !_closed && ReferenceEquals(session, _session))
          UpdateTargetStatus();
      }
      else if (EditorSearchAsync != null)
      {
        await EditorSearchAsync(query, _editorScope == 0, action);
        if (_closed || version != _targetVersion) ClearEditorHighlights?.Invoke();
      }
    }
    catch (Exception exception) { ShowSearchError(exception); }
    finally { _busy = false; if (!_closed) UpdateReplacementState(); }
  }

  private async Task RunReplaceAsync(bool replaceAll)
  {
    if (_busy || _closed || string.IsNullOrEmpty(SearchInput.Text)) return;
    try
    {
      SynchronizeTarget();
      if (!ReplacementRow.IsEnabled) return;
      ClearResultRows();
      _busy = true;
      UpdateReplacementState();
      long version = _targetVersion;
      var session = _session;
      if (session != null)
      {
        await session.ReplaceAsync(GetQuery(), ReplacementInput.Text, replaceAll, GetDocumentId());
        if (!_closed && ReferenceEquals(session, _session)) UpdateTargetStatus();
      }
      else if (EditorReplaceAsync != null)
      {
        await EditorReplaceAsync(GetQuery(), ReplacementInput.Text, _editorScope == 0, replaceAll);
        if (_closed || version != _targetVersion) ClearEditorHighlights?.Invoke();
        if (_closed) ClearReplacementHighlights?.Invoke();
      }
    }
    catch (Exception exception) { ShowSearchError(exception); }
    finally { _busy = false; if (!_closed) UpdateReplacementState(); }
  }

  private void ClearSearchResults()
  {
    _session?.Dispose();
    _session = _target == null ? null : new TextSearchSession(_target);
    if (_target == null) ClearEditorHighlights?.Invoke();
    ResetStatus();
  }

  private void ResetStatus()
  {
    ClearResultRows();
    SearchStatus.Text = "0 из 0";
    SearchStatus.ToolTip = null;
  }

  private void UpdateTargetStatus() => SearchStatus.Text = _session == null || _session.Matches.Count == 0
    ? "0 из 0" : $"{_session.CurrentIndex + 1} из {_session.Matches.Count}";

  private void OnEditorProgress(SearchEvents.SearchProgressChanged progress)
  {
    if (!Dispatcher.CheckAccess()) { Dispatcher.InvokeAsync(() => OnEditorProgress(progress)); return; }
    if (_closed || _target != null) return;
    SearchStatus.Text = $"{progress.Current} из {progress.Total}";
  }

  private void ShowSearchError(Exception exception)
  {
    if (_closed) return;
    if (exception is AggregateException aggregate) exception = aggregate.Flatten().InnerExceptions[0];
    ClearSearchResults();
    if (exception is System.Text.RegularExpressions.RegexParseException)
    {
      NotificationHostService.Instance.Show("Поиск", "Некорректное регулярное выражение", NotificationType.Warning);
      return;
    }
    LogException("Ошибка поиска или замены", exception);
    SearchStatus.Text = exception is System.Text.RegularExpressions.RegexMatchTimeoutException ? "Тайм-аут" : "Ошибка";
    SearchStatus.ToolTip = exception is System.Text.RegularExpressions.RegexMatchTimeoutException
      ? "Выражение выполняется слишком долго. Упростите шаблон." : exception.Message;
  }

  private void OnCloseRequested(SearchEvents.CloseSearchWindow _) => Dispatcher.InvokeAsync(() => { if (!_closed) Close(); });

  private void ReleaseSearch()
  {
    _closed = true;
    ClearReplacementHighlights?.Invoke();
    _targetVersion++;
    _session?.Dispose();
    _session = null;
    ClearEditorHighlights?.Invoke();
    MessageEventAdapter.RaiseInfoMessage(string.Empty);
    EventAggregator.Unsubscribe<SearchEvents.SearchProgressChanged>(OnEditorProgress);
    EventAggregator.Unsubscribe<SearchEvents.CloseSearchWindow>(OnCloseRequested);
  }

  private async void Previous_Click(object sender, RoutedEventArgs e) => await RunSearchAsync(TextSearchAction.Previous);
  private async void Next_Click(object sender, RoutedEventArgs e) => await RunSearchAsync(TextSearchAction.Next);
  private async void Replace_Click(object sender, RoutedEventArgs e) => await RunReplaceAsync(false);
  private async void ReplaceAll_Click(object sender, RoutedEventArgs e) => await RunReplaceAsync(true);
}
