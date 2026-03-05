using System.Windows;
using System.Windows.Controls;

namespace Ask.UI.Shared.Components.Icons
{
  /// <summary>
  /// Логика взаимодействия для PauseIcon.xaml
  /// </summary>
  public partial class PauseIcon : UserControl
  {
    public static readonly DependencyProperty SizeProperty =
               DependencyProperty.Register(nameof(Size), typeof(double), typeof(PauseIcon),
                   new PropertyMetadata(24.0, OnSizeChanged));

    public double Size
    {
      get => (double)GetValue(SizeProperty);
      set => SetValue(SizeProperty, value);
    }

    public PauseIcon()
    {
      InitializeComponent();
    }

    private static void OnSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      if (d is PauseIcon icon)
      {
        icon.Width = icon.Size;
        icon.Height = icon.Size;
      }
    }
  }
}

