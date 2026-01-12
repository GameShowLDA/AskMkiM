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
      // Определяем количество
      _warningTotal = _allIssues.Count(i => i.IsWarning);
      _errorTotal = _allIssues.Count(i => !i.IsWarning);

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
}
