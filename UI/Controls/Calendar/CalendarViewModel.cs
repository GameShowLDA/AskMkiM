using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Input;

namespace UI.Controls.Calendar
{
  public class CalendarDay : INotifyPropertyChanged
  {
    public int DayNumber { get; set; }
    public bool IsCurrentMonth { get; set; }
    public bool IsToday { get; set; }

    public event PropertyChangedEventHandler PropertyChanged;
  }

  public class CalendarViewModel : INotifyPropertyChanged
  {
    private DateTime _displayDate;

    public event PropertyChangedEventHandler PropertyChanged;

    public ObservableCollection<CalendarDay> Days { get; } = new();

    public string MonthYear => _displayDate.ToString("MMMM yyyy", CultureInfo.GetCultureInfo("ru-RU"));

    public ICommand PrevMonthCommand { get; }
    public ICommand NextMonthCommand { get; }

    public CalendarViewModel()
    {
      _displayDate = DateTime.Today;
      PrevMonthCommand = new RelayCommand(_ => ChangeMonth(-1));
      NextMonthCommand = new RelayCommand(_ => ChangeMonth(1));
      BuildCalendar();
    }

    private void ChangeMonth(int offset)
    {
      _displayDate = _displayDate.AddMonths(offset);
      BuildCalendar();
      NotifyPropertyChanged(nameof(MonthYear));
    }

    private void BuildCalendar()
    {
      Days.Clear();

      var firstDayOfMonth = new DateTime(_displayDate.Year, _displayDate.Month, 1);
      int startOffset = ((int)firstDayOfMonth.DayOfWeek + 6) % 7; // Понедельник = 0

      int daysInPrevMonth = DateTime.DaysInMonth(_displayDate.AddMonths(-1).Year, _displayDate.AddMonths(-1).Month);
      int daysInMonth = DateTime.DaysInMonth(_displayDate.Year, _displayDate.Month);
      var today = DateTime.Today;

      // Заполняем дни предыдущего месяца (если есть)
      for (int i = startOffset - 1; i >= 0; i--)
      {
        Days.Add(new CalendarDay
        {
          DayNumber = daysInPrevMonth - i,
          IsCurrentMonth = false,
          IsToday = false
        });
      }

      // Заполняем дни текущего месяца
      for (int day = 1; day <= daysInMonth; day++)
      {
        Days.Add(new CalendarDay
        {
          DayNumber = day,
          IsCurrentMonth = true,
          IsToday = today.Day == day && today.Month == _displayDate.Month && today.Year == _displayDate.Year
        });
      }

      // Заполняем дни следующего месяца чтобы заполнить сетку до 42 дней
      while (Days.Count < 42)
      {
        int nextDay = Days.Count - startOffset - daysInMonth + 1;
        Days.Add(new CalendarDay
        {
          DayNumber = nextDay,
          IsCurrentMonth = false,
          IsToday = false
        });
      }

      NotifyPropertyChanged(nameof(Days));
    }

    private void NotifyPropertyChanged(string propName)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
    }
  }

  // RelayCommand для простых команд
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

    public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

    public void Execute(object? parameter) => _execute(parameter);

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
  }
}
