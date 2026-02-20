using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace Ask.UI.Shared.Components.Icons
{
  public partial class EditIcon : UserControl
  {
    public static readonly DependencyProperty SizeProperty =
      DependencyProperty.Register(
        nameof(Size),
        typeof(double),
        typeof(EditIcon),
        new PropertyMetadata(16d));

    public static readonly DependencyProperty IsWritingProperty =
      DependencyProperty.Register(
        nameof(IsWriting),
        typeof(bool),
        typeof(EditIcon),
        new PropertyMetadata(false, OnIsWritingChanged));

    public EditIcon()
    {
      InitializeComponent();
    }

    public double Size
    {
      get => (double)GetValue(SizeProperty);
      set => SetValue(SizeProperty, value);
    }

    public bool IsWriting
    {
      get => (bool)GetValue(IsWritingProperty);
      set => SetValue(IsWritingProperty, value);
    }

    private static void OnIsWritingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      if (d is EditIcon control && e.NewValue is bool isWriting)
      {
        control.AnimateWriting(isWriting);
      }
    }

    private void AnimateWriting(bool isWriting)
    {
      if (Resources["WriteStartStoryboard"] is not Storyboard writeStart ||
          Resources["WriteStopStoryboard"] is not Storyboard writeStop)
      {
        return;
      }

      if (isWriting)
      {
        writeStop.Stop(this);
        writeStart.Begin(this, true);
      }
      else
      {
        writeStart.Stop(this);
        writeStop.Begin(this, true);
      }
    }
  }
}
