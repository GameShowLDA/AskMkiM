using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Ask.UI.Shared.Components.Icons
{
  /// <summary>
  /// Иконка крестика с возможностью настройки цветов и размера.
  /// </summary>
  public partial class CrossIcon : UserControl
  {
    public CrossIcon()
    {
      InitializeComponent();
      Width = Size;
      Height = Size;
    }

    public static readonly DependencyProperty SizeProperty =
        DependencyProperty.Register(nameof(Size), typeof(double), typeof(CrossIcon),
            new PropertyMetadata(64.0, OnSizeChanged));

    public double Size
    {
      get => (double)GetValue(SizeProperty);
      set => SetValue(SizeProperty, value);
    }

    private static void OnSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      if (d is CrossIcon icon)
      {
        icon.Width = icon.Size;
        icon.Height = icon.Size;
      }
    }

    public static readonly DependencyProperty CircleColorProperty =
        DependencyProperty.Register(nameof(CircleColor), typeof(Brush), typeof(CrossIcon),
            new PropertyMetadata(new SolidColorBrush(Colors.Red)));

    public Brush CircleColor
    {
      get => (Brush)GetValue(CircleColorProperty);
      set => SetValue(CircleColorProperty, value);
    }

    public static readonly DependencyProperty IconStrokeColorProperty =
        DependencyProperty.Register(nameof(IconStrokeColor), typeof(Brush), typeof(CrossIcon),
            new PropertyMetadata(new SolidColorBrush(Color.FromRgb(0x1E, 0x1E, 0x1E)))); // #1E1E1E

    public Brush IconStrokeColor
    {
      get => (Brush)GetValue(IconStrokeColorProperty);
      set => SetValue(IconStrokeColorProperty, value);
    }

    public Brush GetCircleColor() => CircleColor;

    public Brush GetIconStrokeColor() => IconStrokeColor;
  }
}
