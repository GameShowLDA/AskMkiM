using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace Ask.UI.Shared.Components.Icons
{
  public partial class CopyIcon : UserControl
  {
    public static readonly DependencyProperty SizeProperty =
      DependencyProperty.Register(
        nameof(Size),
        typeof(double),
        typeof(CopyIcon),
        new PropertyMetadata(16d));

    public static readonly DependencyProperty IsCopyingProperty =
      DependencyProperty.Register(
        nameof(IsCopying),
        typeof(bool),
        typeof(CopyIcon),
        new PropertyMetadata(false, OnIsCopyingChanged));

    public CopyIcon()
    {
      InitializeComponent();
    }

    public double Size
    {
      get => (double)GetValue(SizeProperty);
      set => SetValue(SizeProperty, value);
    }

    public bool IsCopying
    {
      get => (bool)GetValue(IsCopyingProperty);
      set => SetValue(IsCopyingProperty, value);
    }

    private static void OnIsCopyingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      if (d is CopyIcon control && e.NewValue is bool isCopying)
      {
        control.PlayCopyAnimation(isCopying);
      }
    }

    private void PlayCopyAnimation(bool isCopying)
    {
      if (Resources["CopyInStoryboard"] is not Storyboard copyIn ||
          Resources["CopyOutStoryboard"] is not Storyboard copyOut)
      {
        return;
      }

      if (isCopying)
      {
        copyOut.Stop(this);
        copyIn.Begin(this, true);
      }
      else
      {
        copyIn.Stop(this);
        copyOut.Begin(this, true);
      }
    }
  }
}
