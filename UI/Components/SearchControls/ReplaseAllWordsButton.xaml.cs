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
  /// Логика взаимодействия для ReplaseAllWordsButton.xaml
  /// </summary>
  public partial class ReplaseAllWordsButton : UserControl
  {
    public ReplaseAllWordsButton()
    {
      InitializeComponent();
    }
    private void Border_MouseEnter(object sender, MouseEventArgs e)
    {
      replaceAllWords.Opacity = 1.0;
    }

    private void Border_MouseLeave(object sender, MouseEventArgs e)
    {
      replaceAllWords.Opacity = 0.7;
    }
  }
}
