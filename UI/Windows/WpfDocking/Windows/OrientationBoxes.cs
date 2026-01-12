using System.Windows.Controls;

namespace UI.Windows.WpfDocking.Windows
{
  internal static class OrientationBoxes
  {
    internal static object Vertical = Orientation.Vertical;
    internal static object Horizontal = Orientation.Horizontal;

    internal static object Box(Orientation orientation)
    {
      if (orientation == Orientation.Vertical)
        return Vertical;
      return Horizontal;
    }
  }
}
