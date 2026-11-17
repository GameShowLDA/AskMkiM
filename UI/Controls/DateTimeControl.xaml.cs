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
      CalendarPopup.CustomPopupPlacementCallback = (popupSize, targetSize, offset) =>
        new[] { new System.Windows.Controls.Primitives.CustomPopupPlacement(
          new Point(targetSize.Width - popupSize.Width, -popupSize.Height),
          System.Windows.Controls.Primitives.PopupPrimaryAxis.Horizontal) };
      CalendarControl.CloseCalendarRequested += (_, _) => CalendarPopup.IsOpen = false;
      CalendarPopup.Opened += (_, _) => CalendarControl.Focus();
      Time.ChangeDate += Time_ChangeDate;
      Application.Current.Deactivated += App_Deactivated;
    }

    private async void DateTimeButton_Click(object sender, RoutedEventArgs e)
    {
      if (CalendarPopup.IsOpen)
      {
        CalendarPopup.IsOpen = false;
        return;
      }
      DateTimeButton.IsEnabled = false;
      try
      {
        await CalendarControl.PrepareForOpenAsync();
        CalendarPopup.IsOpen = true;
      }
      finally { DateTimeButton.IsEnabled = true; }
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
