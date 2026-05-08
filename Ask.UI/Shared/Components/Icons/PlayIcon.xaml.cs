using System.Windows;
using System.Windows.Controls;

namespace Ask.UI.Shared.Components.Icons
{
  /// <summary>
  /// Логика взаимодействия для PlayIcon.xaml
  /// </summary>
  public partial class PlayIcon : UserControl
  {
    public static readonly DependencyProperty SizeProperty =
            DependencyProperty.Register(nameof(Size), typeof(double), typeof(PlayIcon),
                new PropertyMetadata(24.0, OnSizeChanged));

    public double Size
    {
      get => (double)GetValue(SizeProperty);
      set => SetValue(SizeProperty, value);
    }

    public PlayIcon()
    {
      InitializeComponent();
    }

    private static void OnSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      if (d is PlayIcon icon)
      {
        icon.Width = icon.Size;
        icon.Height = icon.Size;
      }
    }
  }
}
