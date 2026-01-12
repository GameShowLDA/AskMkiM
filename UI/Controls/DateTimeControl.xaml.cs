using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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
      this.MouseLeftButtonUp += DateTimeControl_MouseLeftButtonUp;
    }

    private void DateTimeControl_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
      CalendarPopup.IsOpen = !CalendarPopup.IsOpen;

    }

    private void Time_ChangeDate()
    {
      Date.Text = DateTime.Now.ToShortDateString();
    }

    // Закрытие при потере фокуса всего приложения
    private void App_Deactivated(object? sender, EventArgs e)
    {
      CalendarPopup.IsOpen = false;
    }
  }
}
