using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace UI.Controls.Calendar
{
  public class CalendarDay : INotifyPropertyChanged
  {
    private bool _isSelected;

    public DateTime Date { get; set; }

    public int DayNumber => Date.Day;

    public bool IsCurrentMonth { get; set; }

    public bool IsToday => Date.Date == DateTime.Today;

    public bool IsSelected
    {
      get => _isSelected;
      set
      {
        if (_isSelected == value)
        {
          return;
        }

        _isSelected = value;
        OnPropertyChanged();
      }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
  }

  public class CalendarViewModel : INotifyPropertyChanged
  {
    private readonly CultureInfo _culture = CultureInfo.GetCultureInfo("ru-RU");
    private DateTime _displayMonth;
    private DateTime _selectedDate;

    public CalendarViewModel()
    {
      _selectedDate = DateTime.Today;
      _displayMonth = new DateTime(_selectedDate.Year, _selectedDate.Month, 1);

      PrevMonthCommand = new RelayCommand(_ => ChangeMonth(-1));
      NextMonthCommand = new RelayCommand(_ => ChangeMonth(1));
      TodayCommand = new RelayCommand(_ => GoToToday());
      SelectDateCommand = new RelayCommand(SelectDate);

      BuildCalendar();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<CalendarDay> Days { get; } = new();

    public string MonthYear => ToTitleCase(_displayMonth.ToString("MMMM yyyy", _culture));

    public string SelectionCaption => ToTitleCase(_selectedDate.ToString("dddd, d MMMM", _culture));

    public ICommand PrevMonthCommand { get; }

    public ICommand NextMonthCommand { get; }

    public ICommand TodayCommand { get; }

    public ICommand SelectDateCommand { get; }

    private void ChangeMonth(int offset)
    {
      _displayMonth = _displayMonth.AddMonths(offset);
      BuildCalendar();
      NotifyHeaderChanged();
    }

    private void GoToToday()
    {
      _selectedDate = DateTime.Today;
      _displayMonth = new DateTime(_selectedDate.Year, _selectedDate.Month, 1);
      BuildCalendar();
      NotifyHeaderChanged();
    }

    private void SelectDate(object? parameter)
    {
      if (parameter is not CalendarDay day)
      {
        return;
      }

      _selectedDate = day.Date.Date;
      _displayMonth = new DateTime(day.Date.Year, day.Date.Month, 1);
      BuildCalendar();
      NotifyHeaderChanged();
    }

    private void BuildCalendar()
    {
      Days.Clear();

      var firstDayOfMonth = new DateTime(_displayMonth.Year, _displayMonth.Month, 1);
      var firstVisibleDay = firstDayOfMonth.AddDays(-((int)firstDayOfMonth.DayOfWeek + 6) % 7);

      for (var index = 0; index < 42; index++)
      {
        var date = firstVisibleDay.AddDays(index);
        Days.Add(new CalendarDay
        {
          Date = date,
          IsCurrentMonth = date.Month == _displayMonth.Month && date.Year == _displayMonth.Year,
          IsSelected = date.Date == _selectedDate.Date,
        });
      }

      OnPropertyChanged(nameof(Days));
    }

    private void NotifyHeaderChanged()
    {
      OnPropertyChanged(nameof(MonthYear));
      OnPropertyChanged(nameof(SelectionCaption));
    }

    private string ToTitleCase(string value)
    {
      return _culture.TextInfo.ToTitleCase(value);
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
  }

  public class RelayCommand : ICommand
  {
    private readonly Action<object?> _execute;
    private readonly Func<object?, bool>? _canExecute;

    public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
    {
      _execute = execute ?? throw new ArgumentNullException(nameof(execute));
      _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter)
    {
      return _canExecute?.Invoke(parameter) ?? true;
    }

    public void Execute(object? parameter)
    {
      _execute(parameter);
    }

    public void RaiseCanExecuteChanged()
    {
      CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
  }
}
