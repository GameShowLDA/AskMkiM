using System.Windows;
using System.Windows.Controls;

namespace UI.Windows.WpfDocking.Windows.Docking.Primitives
{
  /// <summary>Displays the tool window caption.</summary>
  public class ToolWindowCaption : Control
  {
    static ToolWindowCaption()
    {
      DefaultStyleKeyProperty.OverrideMetadata(typeof(ToolWindowCaption), new FrameworkPropertyMetadata(typeof(ToolWindowCaption)));
    }
  }
}
