using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace UI.Components
{
  /// <summary>
  /// Логика взаимодействия для MenuButton.xaml
  /// </summary>
  public partial class MenuButton : UserControl
  {
    public MenuButton()
    {
      InitializeComponent();
    }

    public static readonly DependencyProperty ForegroundProperty =
           DependencyProperty.Register("Foreground", typeof(Brush), typeof(MenuButton), new PropertyMetadata(Brushes.White));

    public Brush Foreground
    {
      get { return (Brush)GetValue(ForegroundProperty); }
      set { SetValue(ForegroundProperty, value); }
    }

    private void Grid_MouseEnter(object sender, MouseEventArgs e)
    {

    }

    private void Grid_MouseLeave(object sender, MouseEventArgs e)
    {

    }
  }
}
