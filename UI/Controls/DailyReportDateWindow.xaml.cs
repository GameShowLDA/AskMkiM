using System.Windows;
using System.IO;
using UI.Controls.Calendar;

namespace UI.Controls;

/// <summary>Выбирает локальную дату диагностического архива.</summary>
public partial class DailyReportDateWindow : Window
{
  public DateTime SelectedDate => ReportCalendar.SelectedDate.Date;

  private readonly string _historyRoot;
  private readonly string _logsRoot;

  public DailyReportDateWindow(string historyRoot, string logsRoot)
  {
    _historyRoot = historyRoot;
    _logsRoot = logsRoot;
    InitializeComponent();
    ReportCalendar.SetAvailabilityProvider(GetAvailability);
    UpdateSelectedDateText();
  }

  private CalendarDayAvailability GetAvailability(DateTime date)
  {
    string day = date.ToString("yyyy-MM-dd");
    bool hasProtocols = HasFiles(Path.Combine(_historyRoot, day));
    bool hasLogs = HasFiles(Path.Combine(_logsRoot, day));
    if (hasProtocols && hasLogs) return CalendarDayAvailability.Complete;
    return hasProtocols || hasLogs ? CalendarDayAvailability.Partial : CalendarDayAvailability.None;
  }

  private static bool HasFiles(string directory)
  {
    try
    {
      return Directory.Exists(directory) && Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories).Any();
    }
    catch (IOException)
    {
      return false;
    }
    catch (UnauthorizedAccessException)
    {
      return false;
    }
  }

  private void Save_Click(object sender, RoutedEventArgs e)
  {
    if (SelectedDate <= DateTime.Today) DialogResult = true;
  }

  private void CalendarButton_Click(object sender, RoutedEventArgs e)
  {
    CalendarPopup.IsOpen = !CalendarPopup.IsOpen;
  }

  private void ReportCalendar_SelectedDateChanged(object? sender, EventArgs e)
  {
    UpdateSelectedDateText();
    CalendarPopup.IsOpen = false;
  }

  private void UpdateSelectedDateText() => SelectedDateText.Text = SelectedDate.ToString("D");

  private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
  {
    if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed) DragMove();
  }
}
