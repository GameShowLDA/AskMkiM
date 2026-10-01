using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace Ask.UI.Shared.Components.Icons
{
  public partial class UploadErrorIcon : UserControl
  {
    public static readonly DependencyProperty SizeProperty =
      DependencyProperty.Register(
        nameof(Size),
        typeof(double),
        typeof(UploadErrorIcon),
        new PropertyMetadata(16d));

    public UploadErrorIcon()
    {
      InitializeComponent();
      var pulse = (Storyboard)Resources["BlinkStoryboard"];
      IsVisibleChanged += (_, _) =>
      {
        if (IsVisible)
        {
          pulse.Begin(this, true);
        }
        else
        {
          pulse.Remove(this);
        }
      };
      Unloaded += (_, _) => pulse.Remove(this);
    }

    public double Size
    {
      get => (double)GetValue(SizeProperty);
      set => SetValue(SizeProperty, value);
    }
  }
}
