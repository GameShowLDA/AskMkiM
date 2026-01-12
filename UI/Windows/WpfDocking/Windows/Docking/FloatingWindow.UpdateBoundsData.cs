using System.Windows;

namespace UI.Windows.WpfDocking.Windows.Docking
{
  public partial class FloatingWindow
  {
    private struct UpdateBoundsData
    {
      public FloatingWindow FloatingWindow;
      public Rect Value;

      public UpdateBoundsData(FloatingWindow floatingWindow, Rect value)
      {
        FloatingWindow = floatingWindow;
        Value = value;
      }
    }
  }
}
