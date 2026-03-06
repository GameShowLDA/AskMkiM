using System.Windows;
using System.Windows.Controls;

namespace Ask.UI.Shared.Components.Icons
{
  public partial class ArchiveIcon : UserControl
  {
    public static readonly DependencyProperty SizeProperty =
      DependencyProperty.Register(
        nameof(Size),
        typeof(double),
        typeof(ArchiveIcon),
        new PropertyMetadata(16d));

    public ArchiveIcon()
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
