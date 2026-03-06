using System.Windows;
using System.Windows.Controls;

namespace Ask.UI.Shared.Components.Icons
{
  public partial class EyeOpenedIcon : UserControl
  {
    public static readonly DependencyProperty SizeProperty =
      DependencyProperty.Register(
        nameof(Size),
        typeof(double),
        typeof(EyeOpenedIcon),
        new PropertyMetadata(16d));

    public EyeOpenedIcon()
    {
      InitializeComponent();
    }

    public double Size
    {
      get => (double)GetValue(SizeProperty);
      set => SetValue(SizeProperty, value);
    }
  }
}
