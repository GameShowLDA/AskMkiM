using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace Ask.UI.Shared.Components.Icons
{
  public partial class PlusCircleIcon : UserControl
  {
    public static readonly DependencyProperty SizeProperty =
      DependencyProperty.Register(
        nameof(Size),
        typeof(double),
        typeof(PlusCircleIcon),
        new PropertyMetadata(16d));

    public static readonly DependencyProperty IsMagnetizedProperty =
      DependencyProperty.Register(
        nameof(IsMagnetized),
        typeof(bool),
        typeof(PlusCircleIcon),
        new PropertyMetadata(false, OnIsMagnetizedChanged));

    public PlusCircleIcon()
    {
      InitializeComponent();
    }

    public double Size
    {
      get => (double)GetValue(SizeProperty);
      set => SetValue(SizeProperty, value);
    }

    public bool IsMagnetized
    {
      get => (bool)GetValue(IsMagnetizedProperty);
      set => SetValue(IsMagnetizedProperty, value);
    }

    private static void OnIsMagnetizedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      if (d is PlusCircleIcon control && e.NewValue is bool isMagnetized)
      {
        control.PlayMagnetAnimation(isMagnetized);
      }
    }

    private void PlayMagnetAnimation(bool isMagnetized)
    {
      if (Resources["MagnetInStoryboard"] is not Storyboard magnetIn ||
          Resources["MagnetOutStoryboard"] is not Storyboard magnetOut)
      {
        return;
      }

      if (isMagnetized)
      {
        magnetOut.Stop(this);
        magnetIn.Begin(this, true);
      }
      else
      {
        magnetIn.Stop(this);
        magnetOut.Begin(this, true);
      }
    }
  }
}
