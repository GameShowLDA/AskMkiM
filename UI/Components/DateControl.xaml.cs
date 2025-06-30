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
using YamlDotNet.Core.Tokens;

namespace UI.Components
{
  /// <summary>
  /// Логика взаимодействия для DateControl.xaml
  /// </summary>
  public partial class DateControl : UserControl
  {
    public DateControl()
    {
      InitializeComponent();
      Loaded += DateControl_Loaded;
      Application.Current.Deactivated += App_Deactivated;
    }

    private void DateControl_Loaded(object sender, RoutedEventArgs e)
    {
      Text = DateTime.Now.ToShortDateString();
    }

    public string Text
    {
      get => Date.Text;
      set => Date.Text = value;
    }

    private void Date_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
      CalendarPopup.IsOpen = !CalendarPopup.IsOpen;
    }

    // Закрытие при потере фокуса всего приложения
    private void App_Deactivated(object? sender, EventArgs e)
    {
      CalendarPopup.IsOpen = false;
    }
  }
}
