using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace Ask.UI.Shared.Components.Icons
{
  public partial class CutIcon : UserControl
  {
    public static readonly DependencyProperty SizeProperty =
      DependencyProperty.Register(
        nameof(Size),
        typeof(double),
        typeof(CutIcon),
        new PropertyMetadata(16d));

    public static readonly DependencyProperty IsCuttingProperty =
      DependencyProperty.Register(
        nameof(IsCutting),
        typeof(bool),
        typeof(CutIcon),
        new PropertyMetadata(false, OnIsCuttingChanged));

    public CutIcon()
    {
      InitializeComponent();
    }

    public double Size
    {
      get => (double)GetValue(SizeProperty);
      set => SetValue(SizeProperty, value);
    }

    public bool IsCutting
    {
      get => (bool)GetValue(IsCuttingProperty);
      set => SetValue(IsCuttingProperty, value);
    }

    private static void OnIsCuttingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      if (d is CutIcon control && e.NewValue is bool isCutting)
      {
        control.PlayCutAnimation(isCutting);
      }
    }

    private void PlayCutAnimation(bool isCutting)
    {
      if (Resources["CutInStoryboard"] is not Storyboard cutIn ||
          Resources["CutOutStoryboard"] is not Storyboard cutOut)
      {
        return;
      }

      if (isCutting)
      {
        cutOut.Stop(this);
        cutIn.Begin(this, true);
      }
      else
      {
        cutIn.Stop(this);
        cutOut.Begin(this, true);
      }
    }
  }
}
