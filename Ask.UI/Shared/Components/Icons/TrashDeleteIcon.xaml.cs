using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace Ask.UI.Shared.Components.Icons
{
  public partial class TrashDeleteIcon : UserControl
  {
    public static readonly DependencyProperty SizeProperty =
      DependencyProperty.Register(
        nameof(Size),
        typeof(double),
        typeof(TrashDeleteIcon),
        new PropertyMetadata(16d));

    public static readonly DependencyProperty IsLidOpenProperty =
      DependencyProperty.Register(
        nameof(IsLidOpen),
        typeof(bool),
        typeof(TrashDeleteIcon),
        new PropertyMetadata(false, OnIsLidOpenChanged));

    public TrashDeleteIcon()
    {
      InitializeComponent();
    }

    public double Size
    {
      get => (double)GetValue(SizeProperty);
      set => SetValue(SizeProperty, value);
    }

    public bool IsLidOpen
    {
      get => (bool)GetValue(IsLidOpenProperty);
      set => SetValue(IsLidOpenProperty, value);
    }

    private static void OnIsLidOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      if (d is TrashDeleteIcon control && e.NewValue is bool isOpen)
      {
        control.AnimateLid(isOpen);
      }
    }

    private void AnimateLid(bool isOpen)
    {
      string key = isOpen ? "LidOpenStoryboard" : "LidCloseStoryboard";
      if (Resources[key] is Storyboard storyboard)
      {
        storyboard.Begin(this, true);
      }
    }
  }
}
