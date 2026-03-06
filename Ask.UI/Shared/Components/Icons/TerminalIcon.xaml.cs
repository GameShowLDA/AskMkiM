using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace Ask.UI.Shared.Components.Icons
{
  public partial class TerminalIcon : UserControl
  {
    public static readonly DependencyProperty SizeProperty =
      DependencyProperty.Register(
        nameof(Size),
        typeof(double),
        typeof(TerminalIcon),
        new PropertyMetadata(16d));

    public static readonly DependencyProperty IsHoveringProperty =
      DependencyProperty.Register(
        nameof(IsHovering),
        typeof(bool),
        typeof(TerminalIcon),
        new PropertyMetadata(false, OnIsHoveringChanged));

    public TerminalIcon()
    {
      InitializeComponent();
    }

    public double Size
    {
      get => (double)GetValue(SizeProperty);
      set => SetValue(SizeProperty, value);
    }

    public bool IsHovering
    {
      get => (bool)GetValue(IsHoveringProperty);
      set => SetValue(IsHoveringProperty, value);
    }

    private static void OnIsHoveringChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      if (d is TerminalIcon control && e.NewValue is bool isHovering)
      {
        control.PlayHoverAnimation(isHovering);
      }
    }

    private void PlayHoverAnimation(bool isHovering)
    {
      if (Resources["HoverInStoryboard"] is not Storyboard hoverIn ||
          Resources["HoverOutStoryboard"] is not Storyboard hoverOut)
      {
        return;
      }

      if (isHovering)
      {
        hoverOut.Stop(this);
        hoverIn.Begin(this, true);
      }
      else
      {
        hoverIn.Stop(this);
        hoverOut.Begin(this, true);
      }
    }
  }
}
