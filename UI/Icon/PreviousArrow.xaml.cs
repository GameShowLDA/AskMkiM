using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace UI.Icon
{
  /// <summary>
  /// Логика взаимодействия для PreviousArrow.xaml
  /// </summary>
  public partial class PreviousArrow : UserControl
  {

    public event Action<MouseButtonEventArgs> ClickMouse;

    public static readonly DependencyProperty SizeProperty =
        DependencyProperty.Register(nameof(Size), typeof(double), typeof(PreviousArrow),
            new PropertyMetadata(24.0, OnSizeChanged));

    public double Size
    {
      get => (double)GetValue(SizeProperty);
      set => SetValue(SizeProperty, value);
    }

    public PreviousArrow()
    {
      InitializeComponent();
    }

    private static void OnSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      if (d is PreviousArrow icon)
      {
        icon.Width = icon.Size;
        icon.Height = icon.Size;
      }
    }

    private void root_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      ClickMouse?.Invoke(e);
    }
  }
}
