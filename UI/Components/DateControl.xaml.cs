using Ask.Core.Services.App;
using System.Windows;
using System.Windows.Controls;

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
    }

    private void DateControl_Loaded(object sender, RoutedEventArgs e)
    {
      Text = ApplicationClockService.CurrentDateTime.ToShortDateString();
    }

    public string Text
    {
      get => Date.Text;
      set => Date.Text = value;
    }
  }
}
