using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace UI.Components
{
  /// <summary>
  /// Логика взаимодействия для BackButton.xaml
  /// </summary>
  public partial class BackButton : UserControl
  {
    public BackButton()
    {
      InitializeComponent();
      var primaryColorBrush = (SolidColorBrush)Application.Current.Resources["BackButtonForegroundColor"];
      this.Foreground = primaryColorBrush;
    }

    public new Brush Foreground
    {
      get { return (Brush)GetValue(ForegroundProperty); }
      set { SetValue(ForegroundProperty, value); }
    }

    public static readonly new DependencyProperty ForegroundProperty =
        DependencyProperty.Register("Foreground", typeof(Brush), typeof(BackButton), new PropertyMetadata(Brushes.Wheat));
  }
}
