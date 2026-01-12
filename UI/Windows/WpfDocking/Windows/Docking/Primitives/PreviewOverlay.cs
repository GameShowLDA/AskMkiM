using System.Windows;
using System.Windows.Controls;

namespace UI.Windows.WpfDocking.Windows.Docking.Primitives
{
  /// <summary>Displays the preview overlay.</summary>
  public class PreviewOverlay : Control
  {
    static PreviewOverlay()
    {
      DefaultStyleKeyProperty.OverrideMetadata(typeof(PreviewOverlay), new FrameworkPropertyMetadata(typeof(PreviewOverlay)));
    }
  }
}
