using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace Ask.UI.Shared.Components.Icons
{
  public partial class ImportConfigIcon : UserControl
  {
    public static readonly DependencyProperty SizeProperty =
      DependencyProperty.Register(
        nameof(Size),
        typeof(double),
        typeof(ImportConfigIcon),
        new PropertyMetadata(16d));

    public static readonly DependencyProperty IsImportingProperty =
      DependencyProperty.Register(
        nameof(IsImporting),
        typeof(bool),
        typeof(ImportConfigIcon),
        new PropertyMetadata(false, OnIsImportingChanged));

    public ImportConfigIcon()
    {
      InitializeComponent();
    }

    public double Size
    {
      get => (double)GetValue(SizeProperty);
      set => SetValue(SizeProperty, value);
    }

    public bool IsImporting
    {
      get => (bool)GetValue(IsImportingProperty);
      set => SetValue(IsImportingProperty, value);
    }

    private static void OnIsImportingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      if (d is ImportConfigIcon control && e.NewValue is bool isImporting)
      {
        control.PlayHoverAnimation(isImporting);
      }
    }

    private void PlayHoverAnimation(bool isImporting)
    {
      if (Resources["HoverInStoryboard"] is not Storyboard hoverIn ||
          Resources["HoverOutStoryboard"] is not Storyboard hoverOut)
      {
        return;
      }

      if (isImporting)
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
