using System.Windows.Controls;

namespace UI.Windows.WpfDocking.Windows.Docking
{
  public partial class DockControl
  {
    private struct ResizeDockTreeData
    {
      public Dock Dock;
      public SplitterDistance Value;

      public ResizeDockTreeData(Dock dock, SplitterDistance value)
      {
        Dock = dock;
        Value = value;
      }
    }
  }
}
