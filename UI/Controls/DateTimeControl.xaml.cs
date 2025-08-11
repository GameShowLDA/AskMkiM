using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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
