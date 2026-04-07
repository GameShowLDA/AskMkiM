using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Errors.Models;
using Ask.Core.Services.EventCore.Events;
using Ask.Core.Services.EventCore.Services;
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
    /// <summary>
    /// Коллекция элементов (ошибки + предупреждения), отображаемая в DataGrid.
    /// </summary>
    public RangeObservableCollection<IDisplayIssue> Items { get; } = new();

    /// <summary>
    /// Коллекция точек остановки для вкладки "Точки остановки".
    /// </summary>
    /// <remarks>Должна обновляться сразу при появлении/удалении/переключении точки.</remarks>
    public ObservableCollection<BreakpointListItem> Breakpoints { get; } = new();

    private readonly List<IDisplayIssue> _allIssues = new();

    private bool _warningsHidden = false;
    private bool _errorsHidden = false;

    private int _warningTotal = 0;
    private int _errorTotal = 0;

    public ErrorListControl()
    {
      InitializeComponent();
      Loaded += ErrorListControl_Loaded;
      DataContext = this;

      EventAggregator.Subscribe<SystemStateEvents.DebugRightsChanged>(e => DebugChanged(e.IsDebug));

      if (AdminConfig.GetDebugRights())
      {
        DebugColumn.Visibility = Visibility.Visible;
      }

      MouseMove += (s, e) =>
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

    private void DebugChanged(bool isDebug)
    {
      Application.Current.Dispatcher.Invoke(() =>
      {
        DebugColumn.Visibility = isDebug ? Visibility.Visible : Visibility.Collapsed;
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
      get => StringsNumber.Visibility;
      set => StringsNumber.Visibility = value;
    }

    public Visibility MeasureResultVisible
    {
      get => MeasureResult.Visibility;
      set => MeasureResult.Visibility = value;
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
      if (!_errorsHidden)
        Items.Add(error);

      ApplyInitialButtonState();

      UpdateTabsVisibilityAndSelection();
    }

    /// <summary>
    /// Добавляет предупреждение.
    /// </summary>
    public void AddWarning(WarningItem warning)
    {
      _allIssues.Add(warning);
      _warningTotal++;
      if (!_warningsHidden)
        Items.Add(warning);

      ApplyInitialButtonState();

      UpdateTabsVisibilityAndSelection();
    }

    /// <summary>
    /// Добавляет список ошибок.
    /// </summary>
    public void AddErrors(IEnumerable<ErrorItem> errors)
    {
      foreach (var err in errors)
      {
        _allIssues.Add(err);
        _errorTotal++;
        if (!_errorsHidden)
          Items.Add(err);
      }

      ApplyInitialButtonState();

      UpdateTabsVisibilityAndSelection();
    }

    /// <summary>
    /// Добавляет список предупреждений.
    /// </summary>
    public void AddWarnings(IEnumerable<WarningItem> warnings)
    {
      foreach (var warn in warnings)
      {
        _allIssues.Add(warn);
        _warningTotal++;
        if (!_warningsHidden)
          Items.Add(warn);
      }

      ApplyInitialButtonState();

      UpdateTabsVisibilityAndSelection();
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
      ReplaceVisibleItems();
      ApplyInitialButtonState();
    }

    public void ClearAll()
    {
      _allIssues.Clear();
      Items.Clear();

      _errorTotal = 0;
      _warningTotal = 0;

      UpdateButtons();

      UpdateTabsVisibilityAndSelection();
    }

    /// <summary>
    /// Событие вызывается при двойном клике по строке с ошибкой или предупреждением.
    /// </summary>
    public event Action<IDisplayIssue>? ItemDoubleClicked;

    private void ApplyFilter()
    {
      Items.Clear();

      foreach (var issue in _allIssues)
      {
        if (issue.IsWarning && _warningsHidden)
          continue;

        if (!issue.IsWarning && _errorsHidden)
          continue;

        Items.Add(issue);
      }

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
          $"{(_warningsHidden ? 0 : _warningTotal)} из {_warningTotal} предупреждений";

      ErrorsButton.Content =
          $"{(_errorsHidden ? 0 : _errorTotal)} из {_errorTotal} ошибок";
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
      ApplyInitialButtonState();
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

    private void ReplaceVisibleItems()
    {
      Items.ReplaceRange(_allIssues.Where(ShouldDisplay));
    }


    private void ApplyInitialButtonState()
    {
      _warningTotal = _allIssues.Count(i => i.IsWarning);
      _errorTotal = _allIssues.Count(i => !i.IsWarning);

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

      UpdateTabsVisibilityAndSelection();
    }

    private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
      if (sender is DataGrid grid && grid.SelectedItem is IDisplayIssue selectedError)
      {
        ItemDoubleClicked?.Invoke(selectedError);
      }
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
}
