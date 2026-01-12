using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Message.Icon
{
  /// <summary>
  /// Логика взаимодействия для WarningIcon.xaml
  /// </summary>
  public partial class WarningIcon : UserControl
  {
    public WarningIcon()
    {
      InitializeComponent();
      Width = Size;
      Height = Size;
    }

    public static readonly DependencyProperty SizeProperty =
        DependencyProperty.Register(nameof(Size), typeof(double), typeof(WarningIcon),
            new PropertyMetadata(64.0, OnSizeChanged));

    public double Size
    {
      get => (double)GetValue(SizeProperty);
      set => SetValue(SizeProperty, value);
    }

    private static void OnSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      if (d is WarningIcon icon)
      {
        icon.Width = icon.Size;
        icon.Height = icon.Size;
      }
    }

    public static readonly DependencyProperty CircleColorProperty =
        DependencyProperty.Register(nameof(CircleColor), typeof(Brush), typeof(WarningIcon),
            new PropertyMetadata(new SolidColorBrush(Colors.Gold)));

    public Brush CircleColor
    {
      get => (Brush)GetValue(CircleColorProperty);
      set => SetValue(CircleColorProperty, value);
    }

    public static readonly DependencyProperty IconFillColorProperty =
        DependencyProperty.Register(nameof(IconStrokeColor), typeof(Brush), typeof(WarningIcon),
            new PropertyMetadata(new SolidColorBrush(Colors.Black)));

    public Brush IconStrokeColor
    {
      get => (Brush)GetValue(IconFillColorProperty);
      set => SetValue(IconFillColorProperty, value);
    }

    public Brush GetCircleColor() => CircleColor;

    public Brush GetIconFillColor() => IconStrokeColor;
  }
}
