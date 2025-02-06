using System.Windows;

namespace Mode.Base.SearchDevices
{
  /// <summary>
  /// Логика взаимодействия для SearchDevices.xaml
  /// </summary>
  public partial class SearchDevices : Window
  {
    public SearchDevices()
    {
      InitializeComponent();
    }

    public void SetDescription(string text)
    {
      header.Text = text;
    }
  }
}
