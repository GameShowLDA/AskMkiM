using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace UI.Controls.Calendar
{
  public class CalendarDay : INotifyPropertyChanged
  {
    private bool _isSelected;
    private bool _hasNote;
    private string _noteText = string.Empty;

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

    public bool HasNote
    {
      get => _hasNote;
      set
      {
        if (_hasNote == value)
        {
          return;
        }

        _hasNote = value;
        OnPropertyChanged();
      }
    }

    public string NoteText
    {
      get => _noteText;
      set
      {
        if (_noteText == value)
        {
          return;
        }

        _noteText = value;
        OnPropertyChanged();
        OnPropertyChanged(nameof(HasTooltip));
      }
    }

    public bool HasTooltip => !string.IsNullOrWhiteSpace(NoteText);

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
  }

  public class CalendarViewModel : INotifyPropertyChanged
  {
    private readonly CultureInfo _culture = CultureInfo.GetCultureInfo("ru-RU");
    private readonly CalendarNoteStore _noteStore = new();
    private readonly Dictionary<DateTime, string> _notes;
    private DateTime _displayMonth;
    private DateTime _selectionStart;
    private DateTime _selectionEnd;

    public CalendarViewModel()
    {
      _selectionStart = DateTime.Today;
      _selectionEnd = DateTime.Today;
      _displayMonth = new DateTime(_selectionStart.Year, _selectionStart.Month, 1);
      _notes = _noteStore.Load();

      PrevMonthCommand = new RelayCommand(_ => ChangeMonth(-1));
      NextMonthCommand = new RelayCommand(_ => ChangeMonth(1));
      TodayCommand = new RelayCommand(_ => GoToToday());

      BuildCalendar();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<CalendarDay> Days { get; } = new();

    public string MonthYear => ToTitleCase(_displayMonth.ToString("MMMM yyyy", _culture));

    public string SelectionCaption => BuildSelectionCaption();

    public ICommand PrevMonthCommand { get; }

    public ICommand NextMonthCommand { get; }

    public ICommand TodayCommand { get; }

    private void ChangeMonth(int offset)
    {
      _displayMonth = _displayMonth.AddMonths(offset);
      BuildCalendar();
      NotifyHeaderChanged();
    }

    private void GoToToday()
    {
      _selectionStart = DateTime.Today;
      _selectionEnd = DateTime.Today;
      _displayMonth = new DateTime(_selectionStart.Year, _selectionStart.Month, 1);
      BuildCalendar();
      NotifyHeaderChanged();
    }

    public void BeginSelection(CalendarDay day)
    {
      SetSelection(day.Date.Date, day.Date.Date, bringMonthToSelection: false);
    }

    public void UpdateSelection(CalendarDay day)
    {
      SetSelection(_selectionStart, day.Date.Date, bringMonthToSelection: false);
    }

    public void CompleteSelection(CalendarDay day)
    {
      SetSelection(_selectionStart, day.Date.Date, bringMonthToSelection: true);
    }

    public void PrepareContextSelection(CalendarDay day)
    {
      if (!IsDateInSelection(day.Date.Date))
      {
        SetSelection(day.Date.Date, day.Date.Date, bringMonthToSelection: false);
      }
    }

    public string GetSelectionNoteSummary()
    {
      var selectedDates = GetSelectedDates().ToList();
      var selectedNotes = selectedDates
        .Where(date => _notes.ContainsKey(date))
        .Select(date => _notes[date])
        .Distinct(StringComparer.Ordinal)
        .ToList();

      if (selectedNotes.Count == 1)
      {
        return selectedNotes[0];
      }

      return string.Empty;
    }

    public bool SelectionHasNotes()
    {
      return GetSelectedDates().Any(date => _notes.ContainsKey(date));
    }

    public void SaveNoteForSelection(string noteText)
    {
      var normalizedText = noteText.Trim();
      foreach (var date in GetSelectedDates())
      {
        _notes[date] = normalizedText;
      }

      _noteStore.Save(_notes);
      BuildCalendar();
    }

    public void DeleteNoteForSelection()
    {
      var changed = false;
      foreach (var date in GetSelectedDates().ToList())
      {
        changed |= _notes.Remove(date);
      }

      if (!changed)
      {
        return;
      }

      _noteStore.Save(_notes);
      BuildCalendar();
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
          IsSelected = IsDateInSelection(date.Date),
          HasNote = _notes.ContainsKey(date.Date),
          NoteText = _notes.TryGetValue(date.Date, out var noteText) ? noteText : string.Empty,
        });
      }

      OnPropertyChanged(nameof(Days));
    }

    private void NotifyHeaderChanged()
    {
      OnPropertyChanged(nameof(MonthYear));
      OnPropertyChanged(nameof(SelectionCaption));
    }

    private string BuildSelectionCaption()
    {
      var start = GetSelectionMin();
      var end = GetSelectionMax();

      if (start == end)
      {
        return ToTitleCase(start.ToString("dddd, d MMMM", _culture));
      }

      return $"{start:dd.MM.yyyy} - {end:dd.MM.yyyy}";
    }

    private void SetSelection(DateTime start, DateTime end, bool bringMonthToSelection)
    {
      _selectionStart = start.Date;
      _selectionEnd = end.Date;

      if (bringMonthToSelection)
      {
        var firstSelectedDay = GetSelectionMin();
        _displayMonth = new DateTime(firstSelectedDay.Year, firstSelectedDay.Month, 1);
      }

      BuildCalendar();
      NotifyHeaderChanged();
    }

    private IEnumerable<DateTime> GetSelectedDates()
    {
      var start = GetSelectionMin();
      var end = GetSelectionMax();

      for (var date = start; date <= end; date = date.AddDays(1))
      {
        yield return date;
      }
    }

    private bool IsDateInSelection(DateTime date)
    {
      var currentDate = date.Date;
      return currentDate >= GetSelectionMin() && currentDate <= GetSelectionMax();
    }

    private DateTime GetSelectionMin()
    {
      return _selectionStart <= _selectionEnd ? _selectionStart : _selectionEnd;
    }

    private DateTime GetSelectionMax()
    {
      return _selectionStart >= _selectionEnd ? _selectionStart : _selectionEnd;
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
