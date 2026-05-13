using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Ask.UI.Shared.Components.Icons
{
  /// <summary>
  /// Логика взаимодействия для TiangleWarningIcon.xaml
  /// </summary>
  public partial class TiangleWarningIcon : UserControl
  {
    public TiangleWarningIcon()
    {
      InitializeComponent();
      Width = Size;
      Height = Size;
    }
    public static readonly DependencyProperty SizeProperty =
        DependencyProperty.Register(nameof(Size), typeof(double), typeof(TiangleWarningIcon),
            new PropertyMetadata(64.0, OnSizeChanged));

    public double Size
    {
      get => (double)GetValue(SizeProperty);
      set => SetValue(SizeProperty, value);
    }

    private static void OnSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      if (d is TiangleWarningIcon icon)
      {
        icon.Width = icon.Size;
        icon.Height = icon.Size;
      }
    }

    public static readonly DependencyProperty TriangleColorProperty =
        DependencyProperty.Register(nameof(TriangleColor), typeof(Brush), typeof(TiangleWarningIcon),
            new PropertyMetadata(new SolidColorBrush(Colors.Gold)));

    public Brush TriangleColor
    {
      get => (Brush)GetValue(TriangleColorProperty);
      set => SetValue(TriangleColorProperty, value);
    }

    public static readonly DependencyProperty IconFillColorProperty =
        DependencyProperty.Register(nameof(IconStrokeColor), typeof(Brush), typeof(TiangleWarningIcon),
            new PropertyMetadata(new SolidColorBrush(Colors.Black)));

    public Brush IconStrokeColor
    {
      get => (Brush)GetValue(IconFillColorProperty);
      set => SetValue(IconFillColorProperty, value);
    }

    public Brush GetTriangleColor() => TriangleColor;

    public Brush GetIconFillColor() => IconStrokeColor;
  }
}
