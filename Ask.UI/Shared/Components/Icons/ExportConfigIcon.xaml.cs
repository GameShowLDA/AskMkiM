using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace Ask.UI.Shared.Components.Icons
{
  public partial class ExportConfigIcon : UserControl
  {
    public static readonly DependencyProperty SizeProperty =
      DependencyProperty.Register(
        nameof(Size),
        typeof(double),
        typeof(ExportConfigIcon),
        new PropertyMetadata(16d));

    public static readonly DependencyProperty IsExportingProperty =
      DependencyProperty.Register(
        nameof(IsExporting),
        typeof(bool),
        typeof(ExportConfigIcon),
        new PropertyMetadata(false, OnIsExportingChanged));

    public ExportConfigIcon()
    {
      InitializeComponent();
    }

    public double Size
    {
      get => (double)GetValue(SizeProperty);
      set => SetValue(SizeProperty, value);
    }

    public bool IsExporting
    {
      get => (bool)GetValue(IsExportingProperty);
      set => SetValue(IsExportingProperty, value);
    }

    private static void OnIsExportingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      if (d is ExportConfigIcon control && e.NewValue is bool isExporting)
      {
        control.PlayHoverAnimation(isExporting);
      }
    }

    private void PlayHoverAnimation(bool isExporting)
    {
      if (Resources["HoverInStoryboard"] is not Storyboard hoverIn ||
          Resources["HoverOutStoryboard"] is not Storyboard hoverOut)
      {
        return;
      }

      if (isExporting)
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
