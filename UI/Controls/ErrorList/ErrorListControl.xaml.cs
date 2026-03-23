using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Errors.Models;
using Ask.Core.Services.EventCore.Events;
using Ask.Core.Services.EventCore.Services;
using Ask.Support;
using System.Collections.Specialized;
using System.ComponentModel;
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
    public RangeObservableCollection<IDisplayIssue> Items { get; } = new();

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

    private void DebugChanged(bool isDebug)
    {
      Application.Current.Dispatcher.Invoke(() =>
      {

        if (isDebug)
        {
          DebugColumn.Visibility = Visibility.Visible;
        }
        else
        {
          DebugColumn.Visibility = Visibility.Collapsed;
        }
      });
    }

    public Visibility StringsNumberVisible
    {
      get
      {
        return StringsNumber.Visibility;
      }
      set
      {
        StringsNumber.Visibility = value;
      }
    }

    public Visibility MeasureResultVisible
    {
      get
      {
        return MeasureResult.Visibility;
      }
      set
      {
        MeasureResult.Visibility = value;
      }
    }

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
      AppendIssues(new IDisplayIssue[] { error });
    }

    /// <summary>
    /// Добавляет предупреждение.
    /// </summary>
    public void AddWarning(WarningItem warning)
    {
      AppendIssues(new IDisplayIssue[] { warning });
    }

    /// <summary>
    /// Добавляет список ошибок.
    /// </summary>
    public void AddErrors(IEnumerable<ErrorItem> errors)
    {
      AppendIssues(errors.Cast<IDisplayIssue>());
    }

    /// <summary>
    /// Добавляет список предупреждений.
    /// </summary>
    public void AddWarnings(IEnumerable<WarningItem> warnings)
    {
      AppendIssues(warnings.Cast<IDisplayIssue>());
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
    }


    /// <summary>
    /// Событие вызывается при двойном клике по строке с ошибкой или предупреждением.
    /// </summary>
    public event Action<IDisplayIssue>? ItemDoubleClicked;

    private void ApplyFilter()
    {
      ReplaceVisibleItems();
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

    private void AppendIssues(IEnumerable<IDisplayIssue> issues)
    {
      var issueList = issues?
        .Where(issue => issue != null)
        .ToList()
        ?? new List<IDisplayIssue>();

      if (issueList.Count == 0)
      {
        ApplyInitialButtonState();
        return;
      }

      _allIssues.AddRange(issueList);

      foreach (var issue in issueList)
      {
        if (issue.IsWarning)
          _warningTotal++;
        else
          _errorTotal++;
      }

      var visibleIssues = issueList.Where(ShouldDisplay).ToList();
      Items.AddRange(visibleIssues);

      ApplyInitialButtonState();
    }

    private void ReplaceVisibleItems()
    {
      Items.ReplaceRange(_allIssues.Where(ShouldDisplay));
    }

    private bool ShouldDisplay(IDisplayIssue issue)
    {
      if (issue.IsWarning)
        return !_warningsHidden;

      return !_errorsHidden;
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

    private void ApplyInitialButtonState()
    {
      // Предупреждения
      if (_warningTotal == 0)
      {
        WarningButton.Visibility = Visibility.Collapsed;
        WarningButton.IsChecked = true; // просто для логики, скрывать нечего
      }
      else
      {
        WarningButton.Visibility = Visibility.Visible;

        // показываем все => IsChecked = true
        WarningButton.IsChecked = !_warningsHidden;
      }

      // Ошибки
      if (_errorTotal == 0)
      {
        ErrorsButton.Visibility = Visibility.Collapsed;
        ErrorsButton.IsChecked = true;
      }
      else
      {
        ErrorsButton.Visibility = Visibility.Visible;

        // показываем все => IsChecked = true
        ErrorsButton.IsChecked = !_errorsHidden;
      }

      // Перерисовываем видимость сразу
      UpdateButtons();
    }

    private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
      if (sender is DataGrid grid && grid.SelectedItem is IDisplayIssue selectedError)
      {
        ItemDoubleClicked?.Invoke(selectedError);
      }
    }
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
