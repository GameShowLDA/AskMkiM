using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Errors.Models;
using Ask.Core.Services.EventCore.Events;
using Ask.Core.Services.EventCore.Services;
using Ask.Support;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace UI.Controls.ErrorList
{
  /// <summary>
  /// Логика взаимодействия для ErrorListControl.xaml
  /// </summary>
  public partial class ErrorListControl : UserControl
  {
    /// <summary>
    /// Коллекция элементов (ошибки + предупреждения), отображаемая в DataGrid.
    /// </summary>
    public ObservableCollection<IDisplayIssue> Items { get; } = new();

    /// <summary>
    /// Коллекция точек остановки для вкладки "Точки остановки".
    /// Должна обновляться сразу при появлении/удалении/переключении точки.
    /// </summary>
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
    }

    #region События для вкладки "Точки остановки"

    /// <summary>
    /// Срабатывает при двойном клике по брейкпоинту в таблице (нужно перейти к нему в редакторе).
    /// </summary>
    public event Action<BreakpointListItem>? BreakpointItemDoubleClicked;

    /// <summary>
    /// Срабатывает при изменении состояния чекбокса брейкпоинта (вкл/выкл).
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

    #region Ошибки/предупреждения (старый функционал)

    /// <summary>
    /// Очищает все элементы.
    /// </summary>
    public void Clear()
    {
      Items.Clear();
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
    }

    public void ClearAll()
    {
      _allIssues.Clear();
      Items.Clear();

      _errorTotal = 0;
      _warningTotal = 0;

      UpdateButtons();
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
    }

    private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
      if (sender is DataGrid grid && grid.SelectedItem is IDisplayIssue selectedError)
      {
        ItemDoubleClicked?.Invoke(selectedError);
      }
    }

    #endregion

    #region Точки остановки (новый функционал)

    /// <summary>
    /// Добавляет брейкпоинт в список или обновляет существующий по номеру команды.
    /// </summary>
    public void UpsertBreakpoint(int commandNumber, int? lineNumber, bool isEnabled)
    {
      if (Dispatcher.CheckAccess())
      {
        UpsertBreakpointCore(commandNumber, lineNumber, isEnabled);
        return;
      }

      Dispatcher.Invoke(() => UpsertBreakpointCore(commandNumber, lineNumber, isEnabled));
    }

    private void UpsertBreakpointCore(int commandNumber, int? lineNumber, bool isEnabled)
    {
      var existing = Breakpoints.FirstOrDefault(b => b.CommandNumber == commandNumber);
      if (existing != null)
      {
        existing.RightLine = lineNumber;
        existing.IsEnabled = isEnabled;
        return;
      }

      Breakpoints.Add(new BreakpointListItem(
        commandNumber: commandNumber,
        leftLine: null,
        rightLine: lineNumber,
        isEnabled: isEnabled
      ));
    }

    /// <summary>
    /// Удаляет брейкпоинт из списка по номеру команды.
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
    /// Полностью очищает список брейкпоинтов.
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
    /// Двойной клик по таблице брейкпоинтов -> запрос перехода к месту в редакторе.
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
    /// Клик по чекбоксу брейкпоинта -> запрос включить/выключить.
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
}