using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace UI.Components
{
  /// <summary>
  /// Логика взаимодействия для CloseButton.xaml
  /// </summary>
  public partial class CloseButton : UserControl
  {
    public CloseButton()
    {
      InitializeComponent();
      BackgroundBorder.Background = Brushes.Transparent;
    }

    private void BackgroundBorder_MouseEnter(object sender, MouseEventArgs e)
    {
      BackgroundBorder.Background = Brushes.Red;
    }

    private void BackgroundBorder_MouseLeave(object sender, MouseEventArgs e)
    {
      BackgroundBorder.Background = Brushes.Transparent;
    }
  }
}
