using Ask.Core.Services.App;
using System.Windows;
using System.Windows.Controls;

namespace UI.Controls
{
  /// <summary>
  /// Логика взаимодействия для DateTimeControl.xaml
  /// </summary>
  public partial class DateTimeControl : UserControl
  {
    public DateTimeControl()
    {
      InitializeComponent();
      Time.ChangeDate += Time_ChangeDate;
      Application.Current.Deactivated += App_Deactivated;
    }

    private void DateTimeButton_Click(object sender, RoutedEventArgs e)
    {
      CalendarPopup.IsOpen = !CalendarPopup.IsOpen;
    }

    private void Time_ChangeDate()
    {
      Date.Text = ApplicationClockService.CurrentDateTime.ToShortDateString();
    }

    private void App_Deactivated(object? sender, EventArgs e)
    {
      CalendarPopup.IsOpen = false;
    }
  }
}
