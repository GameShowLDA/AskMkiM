using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace UI.Icon
{
  /// <summary>
  /// Filled arrow icon rotated to the right. Used for execution pointer visuals.
  /// </summary>
  public partial class ExecutionArrowRightIcon : UserControl
  {
    public static readonly DependencyProperty SizeProperty =
      DependencyProperty.Register(nameof(Size), typeof(double), typeof(ExecutionArrowRightIcon),
        new PropertyMetadata(24.0, OnSizeChanged));

    public static readonly DependencyProperty FillBrushProperty =
      DependencyProperty.Register(nameof(FillBrush), typeof(Brush), typeof(ExecutionArrowRightIcon),
        new PropertyMetadata(Brushes.LimeGreen));

    public double Size
    {
      get => (double)GetValue(SizeProperty);
      set => SetValue(SizeProperty, value);
    }

    public Brush FillBrush
    {
      get => (Brush)GetValue(FillBrushProperty);
      set => SetValue(FillBrushProperty, value);
    }

    public ExecutionArrowRightIcon()
    {
      InitializeComponent();
    }

    private static void OnSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      if (d is ExecutionArrowRightIcon icon)
      {
        icon.Width = icon.Size;
        icon.Height = icon.Size;
      }
    }
  }
}
