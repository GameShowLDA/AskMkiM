using System.Windows.Controls;

namespace UI.Controls.Calendar
{
  /// <summary>
  /// Логика взаимодействия для CalendarControl.xaml
  /// </summary>
  public partial class CalendarControl : UserControl
  {
    public CalendarControl()
    {
      InitializeComponent();
      DataContext = new CalendarViewModel();
    }
  }
}
