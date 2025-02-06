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

namespace UI.Components.SearchControls
{
  /// <summary>
  /// Логика взаимодействия для ExitButton.xaml
  /// </summary>
  public partial class ExitButton : UserControl
  {
    public ExitButton()
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
