using System.Windows;
using System.Windows.Controls;

namespace Ask.UI.Shared.Components.Icons
{
  /// <summary>
  /// Контурная иконка продолжения (стрелка "Play" без заливки).
  /// </summary>
  public partial class ContinueIcon : UserControl
  {
    public static readonly DependencyProperty SizeProperty =
            DependencyProperty.Register(nameof(Size), typeof(double), typeof(ContinueIcon),
                new PropertyMetadata(24.0, OnSizeChanged));

    public double Size
    {
      get => (double)GetValue(SizeProperty);
      set => SetValue(SizeProperty, value);
    }

    public ContinueIcon()
    {
      InitializeComponent();
    }

    private static void OnSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      if (d is ContinueIcon icon)
      {
        icon.Width = icon.Size;
        icon.Height = icon.Size;
      }
    }
  }
}
