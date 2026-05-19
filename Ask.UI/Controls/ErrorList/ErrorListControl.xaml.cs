using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Errors.Models;
using Ask.Core.Services.EventCore.Events;
using Ask.Core.Services.EventCore.Services;
using Ask.UI.Shared.Formatting;
using Ask.Support;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;

namespace Ask.UI.Controls.ErrorList
{
  /// <summary>
  /// Логика взаимодействия для ErrorListControl.xaml
  /// </summary>
  public partial class ErrorListControl : UserControl
  {
    private static readonly object RegistryLock = new();
    private static readonly List<WeakReference<ErrorListControl>> RegisteredControls = new();
    private static WeakReference<ErrorListControl>? _preferredControl;

    /// <summary>
    /// Коллекция элементов (ошибки + предупреждения), отображаемая в DataGrid.
    /// </summary>
    public RangeObservableCollection<IDisplayIssue> Items { get; } = new();

    /// <summary>
    /// Коллекция точек остановки для вкладки "Точки остановки".
    /// </summary>
    /// <remarks>Должна обновляться сразу при появлении/удалении/переключении точки.</remarks>
    public ObservableCollection<BreakpointListItem> Breakpoints { get; } = new();

    public ObservableCollection<PageButtonItem> PageButtons { get; } = new();

    private readonly List<IDisplayIssue> _allIssues = new();

    private const int MinPageSize = 3;
    private const int MaxPageSize = 15;
    private const double IssueRowHeight = 25.0;
    private const double IssueHeaderHeight = 30.0;
    private const double DesiredHeightChrome = 130.0;
    private int _pageSize = MinPageSize;
    private int _currentPageIndex = 0;

    private bool _warningsHidden = false;
    private bool _errorsHidden = false;

    private int _warningTotal = 0;
    private int _errorTotal = 0;

    public ErrorListControl()
    {
      InitializeComponent();
      Loaded += ErrorListControl_Loaded;
      Unloaded += ErrorListControl_Unloaded;
      SizeChanged += ErrorListControl_SizeChanged;
      IsVisibleChanged += ErrorListControl_IsVisibleChanged;
      PreviewMouseDown += (_, _) => PromoteAsPreferredControl();
      GotKeyboardFocus += (_, _) => PromoteAsPreferredControl();
      DataContext = this;
      IssuesTable.IssueNavigationRequested += IssuesTable_IssueNavigationRequested;

      EventAggregator.Subscribe<SystemStateEvents.DebugRightsChanged>(e => DebugChanged(e.IsDebug));

      if (AdminConfig.GetDebugRights())
      {
        IssuesTable.DebugVisible = Visibility.Visible;
      }

      MouseEnter += (s, e) =>
      {
        HelpProvider.SetHelpKey(this, "DescriptionWorkTranslator");
      };

      Items.CollectionChanged += (_, __) => UpdateTabsVisibilityAndSelection();
      Breakpoints.CollectionChanged += (_, __) =>
      {
        UpdateTabsVisibilityAndSelection();
        EnsureBreakpointsSorting();
      };
    }

    public static bool TryNavigatePreferredIssue(int direction)
    {
      var target = GetPreferredControl();
      return target != null && target.NavigateIssues(direction);
    }

    #region События для вкладки "Точки остановки"

    /// <summary>
    /// Срабатывает при двойном клике по точке остановки в таблице (нужно перейти к нему в редакторе).
    /// </summary>
    public event Action<BreakpointListItem>? BreakpointItemDoubleClicked;

    /// <summary>
    /// Срабатывает при изменении состояния чекбокса точки (вкл/выкл).
    /// </summary>
    public event Action<BreakpointListItem, bool>? BreakpointEnabledChanged;

    #endregion

    public event Action<double>? DesiredHeightChanged;

    public void RefreshLayoutFromHost()
    {
      Dispatcher.BeginInvoke(
        new Action(RefreshCurrentPage),
        System.Windows.Threading.DispatcherPriority.Render);
    }

    private void DebugChanged(bool isDebug)
    {
      Application.Current.Dispatcher.Invoke(() =>
      {
        IssuesTable.DebugVisible = isDebug ? Visibility.Visible : Visibility.Collapsed;
      });
    }

    private void UpdateTabsVisibilityAndSelection()
    {
      bool hasBreakpoints = Breakpoints.Count > 0;

      if (!hasBreakpoints && MainTabControl.SelectedItem == BreakpointsTab)
        MainTabControl.SelectedItem = ErrorsTab;
    }

    public Visibility StringsNumberVisible
    {
      get => IssuesTable.StringsNumberVisible;
      set => IssuesTable.StringsNumberVisible = value;
    }

    public Visibility MeasureResultVisible
    {
      get => IssuesTable.MeasureResultVisible;
      set => IssuesTable.MeasureResultVisible = value;
    }

    public bool NavigateIssues(int direction)
    {
      if (!CanNavigateIssues())
        return false;

      var normalizedDirection = direction < 0 ? -1 : 1;
      var targetIssue = GetAdjacentIssue(normalizedDirection);
      if (targetIssue == null)
        return false;

      PromoteAsPreferredControl();
      SelectAndActivateIssue(targetIssue);
      return true;
    }

    #region Ошибки/предупреждения

    /// <summary>
    /// Очищает все элементы.
    /// </summary>
    public void Clear()
    {
      ClearAll();
    }

    /// <summary>
    /// Добавляет ошибку.
    /// </summary>
    public void AddError(ErrorItem error)
    {
      _allIssues.Add(error);
      _errorTotal++;

      RefreshPageIfNewItemIsVisibleOnCurrentPage(error);
      ApplyInitialButtonState();
    }

    /// <summary>
    /// Добавляет предупреждение.
    /// </summary>
    public void AddWarning(WarningItem warning)
    {
      _allIssues.Add(warning);
      _warningTotal++;

      RefreshPageIfNewItemIsVisibleOnCurrentPage(warning);
      ApplyInitialButtonState();
    }

    /// <summary>
    /// Добавляет список ошибок.
    /// </summary>
    public void AddErrors(IEnumerable<ErrorItem> errors)
    {
      var errorList = errors?.ToList() ?? new List<ErrorItem>();
      if (errorList.Count == 0)
        return;

      _allIssues.AddRange(errorList);
      _errorTotal += errorList.Count;

      RefreshCurrentPage();
      ApplyInitialButtonState();
    }

    /// <summary>
    /// Добавляет список предупреждений.
    /// </summary>
    public void AddWarnings(IEnumerable<WarningItem> warnings)
    {
      var warningList = warnings?.ToList() ?? new List<WarningItem>();
      if (warningList.Count == 0)
        return;

      _allIssues.AddRange(warningList);
      _warningTotal += warningList.Count;

      RefreshCurrentPage();
      ApplyInitialButtonState();
    }

    /// <summary>
    /// Полностью заменяет набор отображаемых диагностик.
    /// Используется для пакетной загрузки без множества перерисовок UI.
    /// </summary>
    public void SetIssues(IEnumerable<IDisplayIssue> issues)
    {
      var issueList = issues?.ToList() ?? new List<IDisplayIssue>();

      _allIssues.Clear();
      _allIssues.AddRange(issueList);

      RecalculateTotals();
      _currentPageIndex = 0;
      RefreshCurrentPage();
      ApplyInitialButtonState();
    }

    public void ClearAll()
    {
      _allIssues.Clear();
      Items.Clear();

      _errorTotal = 0;
      _warningTotal = 0;
      _currentPageIndex = 0;

      UpdateButtons();
      UpdateIssueTableHeight();
      UpdatePagingControls();

      UpdateTabsVisibilityAndSelection();
      UpdateDesiredHeight();
    }

    /// <summary>
    /// Событие вызывается при двойном клике по строке с ошибкой или предупреждением.
    /// </summary>
    public event Action<IDisplayIssue>? ItemDoubleClicked;

    private void ApplyFilter()
    {
      _currentPageIndex = 0;
      RefreshCurrentPage();

      UpdateButtons();
      UpdateTabsVisibilityAndSelection();
    }

    private void EnsureBreakpointsSorting()
    {
      var view = CollectionViewSource.GetDefaultView(Breakpoints);
      if (view == null) return;

      using (view.DeferRefresh())
      {
        view.SortDescriptions.Clear();
        view.SortDescriptions.Add(new SortDescription(nameof(BreakpointListItem.RightLine), ListSortDirection.Ascending));
      }
    }

    private void UpdateButtons()
    {
      WarningButton.Content =
          $"{CountDisplayFormatter.Format(_warningsHidden ? 0 : _warningTotal)} из {CountDisplayFormatter.Format(_warningTotal)} предупреждений";

      ErrorsButton.Content =
          $"{CountDisplayFormatter.Format(_errorsHidden ? 0 : _errorTotal)} из {CountDisplayFormatter.Format(_errorTotal)} ошибок";
    }

    private void WarningButton_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      var btn = (ToggleButton)sender;
      btn.IsChecked = !btn.IsChecked;

      _warningsHidden = !(btn.IsChecked == true);
      ApplyFilter();

      e.Handled = true;
    }

    private void ErrorsButton_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      var btn = (ToggleButton)sender;
      btn.IsChecked = !btn.IsChecked;

      _errorsHidden = !(btn.IsChecked == true);
      ApplyFilter();
      e.Handled = true;
    }

    private void ErrorListControl_Loaded(object sender, RoutedEventArgs e)
    {
      RegisterControl(this);
      ApplyInitialButtonState();
      RefreshCurrentPage();
      PromoteAsPreferredControl();
    }

    private void ErrorListControl_Unloaded(object sender, RoutedEventArgs e)
    {
      UnregisterControl(this);
    }

    private void ErrorListControl_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
      if (IsVisible)
      {
        PromoteAsPreferredControl();
      }
    }

    private void IssuesTable_IssueNavigationRequested(object? sender, IssueNavigationRequestedEventArgs e)
    {
      NavigateIssues(e.Direction);
    }

    private IDisplayIssue? GetAdjacentIssue(int direction)
    {
      var visibleIssues = GetVisibleIssues().ToList();
      if (visibleIssues.Count == 0)
        return null;

      var currentIndex = IssuesTable.SelectedIssue == null
        ? -1
        : visibleIssues.IndexOf(IssuesTable.SelectedIssue);

      if (currentIndex < 0)
        return direction > 0 ? visibleIssues[0] : visibleIssues[^1];

      var nextIndex = (currentIndex + direction + visibleIssues.Count) % visibleIssues.Count;
      return visibleIssues[nextIndex];
    }

    private void SelectAndActivateIssue(IDisplayIssue targetIssue)
    {
      var visibleIndex = GetVisibleIssues()
        .Select((issue, index) => new { issue, index })
        .FirstOrDefault(x => ReferenceEquals(x.issue, targetIssue) || Equals(x.issue, targetIssue))
        ?.index;

      if (visibleIndex == null)
        return;

      var targetPageIndex = visibleIndex.Value / _pageSize;
      if (targetPageIndex != _currentPageIndex)
      {
        _currentPageIndex = targetPageIndex;
        RefreshCurrentPage();
      }

      IssuesTable.SelectedIssue = targetIssue;
      ItemDoubleClicked?.Invoke(targetIssue);
      Dispatcher.BeginInvoke(new Action(IssuesTable.FocusTable), System.Windows.Threading.DispatcherPriority.Input);
    }

    private void PromoteAsPreferredControl()
    {
      lock (RegistryLock)
      {
        _preferredControl = new WeakReference<ErrorListControl>(this);
      }
    }

    private static void RegisterControl(ErrorListControl control)
    {
      lock (RegistryLock)
      {
        CleanupRegisteredControls();

        if (RegisteredControls.Any(reference => reference.TryGetTarget(out var existing) && ReferenceEquals(existing, control)))
          return;

        RegisteredControls.Add(new WeakReference<ErrorListControl>(control));
      }
    }

    private static void UnregisterControl(ErrorListControl control)
    {
      lock (RegistryLock)
      {
        RegisteredControls.RemoveAll(reference => !reference.TryGetTarget(out var existing) || ReferenceEquals(existing, control));

        if (_preferredControl != null
            && (!_preferredControl.TryGetTarget(out var preferred) || ReferenceEquals(preferred, control)))
        {
          _preferredControl = null;
        }
      }
    }

    private static ErrorListControl? GetPreferredControl()
    {
      lock (RegistryLock)
      {
        CleanupRegisteredControls();

        if (_preferredControl != null
            && _preferredControl.TryGetTarget(out var preferred)
            && preferred.CanNavigateIssues())
        {
          return preferred;
        }

        var fallback = RegisteredControls
          .Select(reference => reference.TryGetTarget(out var control) ? control : null)
          .Where(control => control != null && control.CanNavigateIssues())
          .OrderBy(control => control!.IsKeyboardFocusWithin)
          .ThenBy(control => Window.GetWindow(control)?.IsActive == true)
          .LastOrDefault();

        if (fallback != null)
        {
          _preferredControl = new WeakReference<ErrorListControl>(fallback);
        }

        return fallback;
      }
    }

    private static void CleanupRegisteredControls()
    {
      RegisteredControls.RemoveAll(reference => !reference.TryGetTarget(out _));
    }

    private IEnumerable<IDisplayIssue> GetVisibleIssues()
    {
      return _allIssues.Where(ShouldDisplay);
    }

    private bool CanNavigateIssues()
    {
      return IsLoaded
             && IsVisible
             && Visibility == Visibility.Visible
             && MainTabControl.SelectedItem == ErrorsTab
             && GetVisibleCount() > 0;
    }

    private void RecalculateTotals()
    {
      _warningTotal = 0;
      _errorTotal = 0;

      foreach (var issue in _allIssues)
      {
        if (issue.IsWarning)
          _warningTotal++;
        else
          _errorTotal++;
      }
    }

    private bool ShouldDisplay(IDisplayIssue issue)
    {
      if (issue.IsWarning)
        return !_warningsHidden;

      return !_errorsHidden;
    }

    private int GetVisibleCount()
    {
      if (_warningsHidden && _errorsHidden)
        return 0;

      if (_warningsHidden)
        return _errorTotal;

      if (_errorsHidden)
        return _warningTotal;

      return _errorTotal + _warningTotal;
    }

    private void ErrorListControl_SizeChanged(object sender, SizeChangedEventArgs e)
    {
      if (Math.Abs(e.NewSize.Height - e.PreviousSize.Height) < IssueRowHeight)
        return;

      RefreshCurrentPage();
    }

    private void UpdatePageSizeFromData()
    {
      var visibleCount = GetVisibleCount();
      var calculatedPageSize = CalculatePageSize(visibleCount);

      if (calculatedPageSize == _pageSize)
        return;

      var firstVisibleItemIndex = _currentPageIndex * _pageSize;
      _pageSize = calculatedPageSize;
      _currentPageIndex = firstVisibleItemIndex / _pageSize;
    }

    private void UpdateDesiredHeight()
    {
      var visibleRows = GetVisibleRowsForLayout();

      DesiredHeightChanged?.Invoke(DesiredHeightChrome + visibleRows * IssueRowHeight);
    }

    private int GetVisibleRowsForLayout()
    {
      var visibleCount = GetVisibleCount();
      return CalculatePageSize(visibleCount);
    }

    private int CalculatePageSize(int visibleCount)
    {
      if (visibleCount == 0)
        return MinPageSize;

      var rowsByHeight = GetRowsByCurrentHeight();
      var maxAllowed = Math.Min(MaxPageSize, visibleCount);

      if (maxAllowed < MinPageSize)
        return maxAllowed;

      return Math.Clamp(rowsByHeight, MinPageSize, maxAllowed);
    }

    private int GetRowsByCurrentHeight()
    {
      if (ActualHeight <= 0)
        return MaxPageSize;

      var rowsAreaHeight = ActualHeight - DesiredHeightChrome;
      if (rowsAreaHeight <= 0)
        return MinPageSize;

      return Math.Clamp((int)Math.Floor(rowsAreaHeight / IssueRowHeight), MinPageSize, MaxPageSize);
    }

    private void UpdateIssueTableHeight()
    {
      if (IssuesTable == null)
        return;

      IssuesTable.Height = IssueHeaderHeight + GetVisibleRowsForLayout() * IssueRowHeight;
    }

    private int GetMaxPageIndex()
    {
      var visibleCount = GetVisibleCount();
      if (visibleCount == 0)
        return 0;

      return (visibleCount - 1) / _pageSize;
    }

    private void NormalizeCurrentPage()
    {
      var maxPageIndex = GetMaxPageIndex();

      if (_currentPageIndex < 0)
        _currentPageIndex = 0;

      if (_currentPageIndex > maxPageIndex)
        _currentPageIndex = maxPageIndex;
    }

    private IEnumerable<IDisplayIssue> GetCurrentPageItems()
    {
      var skip = _currentPageIndex * _pageSize;
      var skipped = 0;
      var taken = 0;

      foreach (var issue in _allIssues)
      {
        if (!ShouldDisplay(issue))
        {
          continue;
        }

        if (skipped < skip)
        {
          skipped++;
          continue;
        }

        yield return issue;
        taken++;

        if (taken >= _pageSize)
        {
          yield break;
        }
      }
    }

    private void RefreshCurrentPage()
    {
      UpdatePageSizeFromData();
      NormalizeCurrentPage();
      Items.ReplaceRange(GetCurrentPageItems());
      UpdateIssueTableHeight();
      UpdatePagingControls();
      UpdateTabsVisibilityAndSelection();
      UpdateDesiredHeight();
    }

    private void RefreshPageIfNewItemIsVisibleOnCurrentPage(IDisplayIssue newIssue)
    {
      if (!ShouldDisplay(newIssue))
      {
        UpdatePagingControls();
        UpdateTabsVisibilityAndSelection();
        return;
      }

      var visibleCount = GetVisibleCount();
      var newItemVisibleIndex = visibleCount - 1;
      var pageStartIndex = _currentPageIndex * _pageSize;
      var pageEndIndex = pageStartIndex + _pageSize - 1;

      if (newItemVisibleIndex >= pageStartIndex && newItemVisibleIndex <= pageEndIndex)
      {
        RefreshCurrentPage();
        return;
      }

      UpdatePagingControls();
      UpdateTabsVisibilityAndSelection();
    }

    private void UpdatePagingControls()
    {
      if (PageInfoText == null || PrevPageButton == null || NextPageButton == null)
        return;

      var visibleCount = GetVisibleCount();
      var maxPageIndex = GetMaxPageIndex();
      var totalPages = visibleCount == 0 ? 0 : maxPageIndex + 1;

      PrevPageButton.IsEnabled = visibleCount > 0 && _currentPageIndex > 0;
      NextPageButton.IsEnabled = visibleCount > 0 && _currentPageIndex < maxPageIndex;

      if (visibleCount == 0)
      {
        PageInfoText.Text = "0 из 0";
        RebuildPageButtons(totalPages);
        return;
      }

      var from = _currentPageIndex * _pageSize + 1;
      var to = Math.Min(from + _pageSize - 1, visibleCount);
      PageInfoText.Text = $"{from}-{to} из {CountDisplayFormatter.Format(visibleCount)}";

      RebuildPageButtons(totalPages);
    }

    private void RebuildPageButtons(int totalPages)
    {
      var nextButtons = totalPages <= 0
        ? new List<PageButtonItem>()
        : BuildVisiblePageItems(totalPages).ToList();

      if (ArePageButtonsEqual(nextButtons))
        return;

      PageButtons.Clear();
      foreach (var page in nextButtons)
        PageButtons.Add(page);
    }

    private bool ArePageButtonsEqual(IReadOnlyList<PageButtonItem> nextButtons)
    {
      if (PageButtons.Count != nextButtons.Count)
        return false;

      for (var i = 0; i < nextButtons.Count; i++)
      {
        var current = PageButtons[i];
        var next = nextButtons[i];
        if (current.PageIndex != next.PageIndex ||
            current.DisplayText != next.DisplayText ||
            current.IsClickable != next.IsClickable ||
            current.IsCurrent != next.IsCurrent)
        {
          return false;
        }
      }

      return true;
    }

    private IEnumerable<PageButtonItem> BuildVisiblePageItems(int totalPages)
    {
      var pageIndexes = new SortedSet<int>();

      if (totalPages <= 7)
      {
        for (var i = 0; i < totalPages; i++)
          pageIndexes.Add(i);
      }
      else
      {
        pageIndexes.Add(0);
        pageIndexes.Add(totalPages - 1);

        for (var i = _currentPageIndex - 1; i <= _currentPageIndex + 1; i++)
        {
          if (i >= 0 && i < totalPages)
            pageIndexes.Add(i);
        }

        if (_currentPageIndex <= 2)
        {
          pageIndexes.Add(1);
          pageIndexes.Add(2);
          pageIndexes.Add(3);
        }

        if (_currentPageIndex >= totalPages - 3)
        {
          pageIndexes.Add(totalPages - 2);
          pageIndexes.Add(totalPages - 3);
          pageIndexes.Add(totalPages - 4);
        }
      }

      int? previous = null;
      foreach (var index in pageIndexes.Where(i => i >= 0 && i < totalPages))
      {
        if (previous.HasValue && index - previous.Value > 1)
        {
          yield return new PageButtonItem
          {
            PageIndex = -1,
            DisplayText = "...",
            IsClickable = false,
            IsCurrent = false
          };
        }

        yield return new PageButtonItem
        {
          PageIndex = index,
          DisplayText = (index + 1).ToString(),
          IsClickable = true,
          IsCurrent = index == _currentPageIndex
        };

        previous = index;
      }
    }

    private void PrevPageButton_Click(object sender, RoutedEventArgs e)
    {
      _currentPageIndex--;
      RefreshCurrentPage();
    }

    private void PageNumberButton_Click(object sender, RoutedEventArgs e)
    {
      if (sender is not Button button || button.Tag is not int pageIndex || pageIndex < 0 || pageIndex == _currentPageIndex)
        return;

      _currentPageIndex = pageIndex;
      RefreshCurrentPage();
    }

    private void NextPageButton_Click(object sender, RoutedEventArgs e)
    {
      _currentPageIndex++;
      RefreshCurrentPage();
    }

    private void ApplyInitialButtonState()
    {
      if (_warningTotal == 0)
      {
        WarningButton.Visibility = Visibility.Collapsed;
        WarningButton.IsChecked = true;
      }
      else
      {
        WarningButton.Visibility = Visibility.Visible;
        WarningButton.IsChecked = !_warningsHidden;
      }

      if (_errorTotal == 0)
      {
        ErrorsButton.Visibility = Visibility.Collapsed;
        ErrorsButton.IsChecked = true;
      }
      else
      {
        ErrorsButton.Visibility = Visibility.Visible;
        ErrorsButton.IsChecked = !_errorsHidden;
      }

      UpdateButtons();
      UpdatePagingControls();

      UpdateTabsVisibilityAndSelection();
    }

    private void IssuesTable_ItemDoubleClicked(IDisplayIssue selectedError)
    {
      ItemDoubleClicked?.Invoke(selectedError);
    }

    #endregion

    #region Точки остановки

    /// <summary>
    /// Добавляет точку остановки в список или обновляет существующий по номеру команды.
    /// </summary>
    public void UpsertBreakpoint(int commandNumber, int rightLine, string Mnemonic, bool isEnabled)
    {
      if (Dispatcher.CheckAccess())
      {
        UpsertBreakpointCore(commandNumber, rightLine, Mnemonic, isEnabled);
        return;
      }

      Dispatcher.Invoke(() => UpsertBreakpointCore(commandNumber, rightLine, Mnemonic, isEnabled));
    }

    private void UpsertBreakpointCore(int commandNumber, int rightLine, string mnemonic, bool isEnabled)
    {
      var existing = Breakpoints.FirstOrDefault(b => b.CommandNumber == commandNumber);
      if (existing != null)
      {
        existing.IsEnabled = isEnabled;
        return;
      }

      Breakpoints.Add(new BreakpointListItem(
        commandNumber: commandNumber,
        leftLine: null,
        rightLine: rightLine,
        isEnabled: isEnabled,
        mnemonic: mnemonic
      ));
    }

    /// <summary>
    /// Удаляет точку остановки из списка по номеру команды.
    /// </summary>
    public void RemoveBreakpoint(int commandNumber)
    {
      if (Dispatcher.CheckAccess())
      {
        RemoveBreakpointCore(commandNumber);
        return;
      }

      Dispatcher.Invoke(() => RemoveBreakpointCore(commandNumber));
    }

    private void RemoveBreakpointCore(int commandNumber)
    {
      var existing = Breakpoints.FirstOrDefault(b => b.CommandNumber == commandNumber);
      if (existing != null)
        Breakpoints.Remove(existing);
    }

    /// <summary>
    /// Полностью очищает список точек остановки.
    /// </summary>
    public void ClearBreakpoints()
    {
      if (Dispatcher.CheckAccess())
      {
        Breakpoints.Clear();
        return;
      }

      Dispatcher.Invoke(() => Breakpoints.Clear());
    }

    /// <summary>
    /// Двойной клик по таблице для перехода на строку с точкой остановки.
    /// </summary>
    private void BreakpointsGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
      if (sender is DataGrid grid && grid.SelectedItem is BreakpointListItem bp)
      {
        BreakpointItemDoubleClicked?.Invoke(bp);
        e.Handled = true;
      }
    }

    /// <summary>
    /// Клик по чекбоксу для включения/отключения точки остановки.
    /// </summary>
    private void BreakpointEnabled_Click(object sender, RoutedEventArgs e)
    {
      if (sender is CheckBox cb && cb.DataContext is BreakpointListItem bp)
      {
        bool enabled = cb.IsChecked == true;
        BreakpointEnabledChanged?.Invoke(bp, enabled);
        e.Handled = true;
      }
    }

    #endregion
  }

  public sealed class RangeObservableCollection<T> : ObservableCollection<T>
  {
    public void AddRange(IEnumerable<T> items)
    {
      var bufferedItems = items?.ToList() ?? new List<T>();
      if (bufferedItems.Count == 0)
        return;

      CheckReentrancy();

      foreach (var item in bufferedItems)
      {
        Items.Add(item);
      }

      RaiseReset();
    }

    public void ReplaceRange(IEnumerable<T> items)
    {
      CheckReentrancy();

      Items.Clear();

      if (items != null)
      {
        foreach (var item in items)
        {
          Items.Add(item);
        }
      }

      RaiseReset();
    }

    private void RaiseReset()
    {
      OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
      OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
      OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }
  }
  public sealed class PageButtonItem
  {
    public int PageIndex { get; init; }

    public string DisplayText { get; init; } = string.Empty;

    public bool IsCurrent { get; init; }

    public bool IsClickable { get; init; } = true;
  }

}
