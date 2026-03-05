using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace Ask.UI.Shared.Components.Icons
{
  public partial class PrintIcon : UserControl
  {
    public static readonly DependencyProperty SizeProperty =
      DependencyProperty.Register(
        nameof(Size),
        typeof(double),
        typeof(PrintIcon),
        new PropertyMetadata(16d));

    public static readonly DependencyProperty IsPrintingProperty =
      DependencyProperty.Register(
        nameof(IsPrinting),
        typeof(bool),
        typeof(PrintIcon),
        new PropertyMetadata(false, OnIsPrintingChanged));

    public PrintIcon()
    {
      InitializeComponent();
    }

    public double Size
    {
      get => (double)GetValue(SizeProperty);
      set => SetValue(SizeProperty, value);
    }

    public bool IsPrinting
    {
      get => (bool)GetValue(IsPrintingProperty);
      set => SetValue(IsPrintingProperty, value);
    }

    private static void OnIsPrintingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      if (d is PrintIcon control && e.NewValue is bool isPrinting)
      {
        control.PlayPrintAnimation(isPrinting);
      }
    }

    private void PlayPrintAnimation(bool isPrinting)
    {
      if (Resources["PrintInStoryboard"] is not Storyboard printIn ||
          Resources["PrintOutStoryboard"] is not Storyboard printOut)
      {
        return;
      }

      if (isPrinting)
      {
        printOut.Stop(this);
        printIn.Begin(this, true);
      }
      else
      {
        printIn.Stop(this);
        printOut.Begin(this, true);
      }
    }
  }
}
