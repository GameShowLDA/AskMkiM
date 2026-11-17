using Ask.Core.Services.App;
using Ask.Core.Services.Calendar;
using System.IO;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using static Ask.LogLib.LoggerUtility;

namespace MainWindowProgram;

public partial class MainWindow
{
  private readonly CalendarNoteService _todayNotes = new();
  private readonly ObservableCollection<CalendarNote> _todayTaskItems = new();
  private readonly DispatcherTimer _todayTasksTimer = new() { Interval = TimeSpan.FromSeconds(30) };
  private bool _todayTasksBusy;
  private bool _todayCompletionInProgress;
  private bool _todayTasksPending;
  private bool _todayTasksClosed;

  private void InitializeTodayTasks()
  {
    TodayTasksList.ItemsSource = _todayTaskItems;
    CalendarNoteService.Changed += OnTodayNotesChanged;
    _todayTasksTimer.Tick += async (_, _) => await RefreshTodayTasksAsync();
    Loaded += async (_, _) => { _todayTasksTimer.Start(); await RefreshTodayTasksAsync(); };
    Activated += async (_, _) => await RefreshTodayTasksAsync();
    Deactivated += (_, _) => TodayTasksPopup.IsOpen = false;
    TodayTasksPopup.PreviewKeyDown += (_, e) =>
    {
      if (e.Key == Key.Escape) { TodayTasksPopup.IsOpen = false; e.Handled = true; }
      else if (_todayCompletionInProgress) e.Handled = true;
    };
    Closed += (_, _) =>
    {
      _todayTasksClosed = true;
      _todayTasksTimer.Stop();
      CalendarNoteService.Changed -= OnTodayNotesChanged;
      TodayTasksPopup.IsOpen = false;
    };
  }

  private void OnTodayNotesChanged()
  {
    if (!Dispatcher.HasShutdownStarted)
      Dispatcher.BeginInvoke(new Action(async () => await RefreshTodayTasksAsync()));
  }

  private async Task RefreshTodayTasksAsync()
  {
    if (_todayTasksClosed) return;
    if (_todayTasksBusy || _todayCompletionInProgress) { _todayTasksPending = true; return; }
    _todayTasksBusy = true;
    try
    {
      var today = ApplicationClockService.CurrentDateTime.Date;
      var tasks = (await Task.Run(() => _todayNotes.Load())).Where(note => note.Date.Date == today)
        .OrderBy(note => note.IsCompleted).ToArray();
      if (_todayTasksClosed) return;
      if (_todayCompletionInProgress) { _todayTasksPending = true; return; }
      var ids = tasks.Select(note => note.Id).ToHashSet();
      for (int index = _todayTaskItems.Count - 1; index >= 0; index--)
        if (!ids.Contains(_todayTaskItems[index].Id)) _todayTaskItems.RemoveAt(index);
      for (int index = 0; index < tasks.Length; index++)
      {
        var existing = _todayTaskItems.FirstOrDefault(note => note.Id == tasks[index].Id);
        int previous = existing is null ? -1 : _todayTaskItems.IndexOf(existing);
        if (previous < 0) _todayTaskItems.Insert(index, tasks[index]);
        else
        {
          if (previous != index) _todayTaskItems.Move(previous, index);
          if (_todayTaskItems[index] != tasks[index]) _todayTaskItems[index] = tasks[index];
        }
      }
      int pending = tasks.Count(note => !note.IsCompleted);
      TodayTasksCount.Text = pending > 9 ? "9+" : pending.ToString();
      TodayTasksCount.Visibility = pending > 0 ? Visibility.Visible : Visibility.Collapsed;
      TodayTasksButton.Visibility = Visibility.Visible;
      TodayTasksButton.ToolTip = tasks.Length == 0 ? "На сегодня дел нет"
        : pending == 0 ? "Все дела на сегодня выполнены" : $"Невыполненные дела на сегодня: {pending}";
      TodayTasksEmpty.Visibility = tasks.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
      TodayTasksError.Text = string.Empty;
    }
    catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException)
    {
      LogException("Не удалось загрузить дела на сегодня", error);
      TodayTasksError.Text = "Не удалось загрузить дела. " + error.Message;
    }
    finally
    {
      _todayTasksBusy = false;
      if (_todayTasksPending && !_todayTasksClosed)
      {
        _todayTasksPending = false;
        await RefreshTodayTasksAsync();
      }
    }
  }

  private async void TodayTasksButton_Click(object sender, RoutedEventArgs e)
  {
    if (TodayTasksPopup.IsOpen) { TodayTasksPopup.IsOpen = false; return; }
    await RefreshTodayTasksAsync();
    if (TodayTasksButton.Visibility == Visibility.Visible) TodayTasksPopup.IsOpen = true;
  }

  private async void TodayTaskCompleted_Click(object sender, RoutedEventArgs e)
  {
    var checkbox = (CheckBox)sender;
    if (_todayCompletionInProgress) { checkbox.SetCurrentValue(CheckBox.IsCheckedProperty, ((CalendarNote)checkbox.Tag).IsCompleted); return; }
    var original = (CalendarNote)checkbox.Tag;
    bool completed = checkbox.IsChecked == true;
    string? saveError = null;
    TodayTasksList.IsHitTestVisible = false;
    _todayCompletionInProgress = true;
    try
    {
      var note = (CalendarNote)checkbox.Tag;
      await Task.WhenAll(Task.Run(() => _todayNotes.SetCompleted(note.Id, completed)), Task.Delay(250));
    }
    catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException)
    {
      checkbox.SetCurrentValue(CheckBox.IsCheckedProperty, original.IsCompleted);
      LogException("Не удалось отметить дело", error);
      saveError = "Не удалось сохранить отметку. " + error.Message;
    }
    finally
    {

      _todayCompletionInProgress = false;
      TodayTasksList.IsHitTestVisible = true;
      await RefreshTodayTasksAsync();
      if (saveError is not null) TodayTasksError.Text = saveError;
    }
  }
}
