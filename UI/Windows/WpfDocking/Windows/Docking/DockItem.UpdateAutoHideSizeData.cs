namespace UI.Windows.WpfDocking.Windows.Docking
{
  public partial class DockItem
  {
    private struct UpdateAutoHideSizeData
    {
      public DockItem DockItem;
      public SplitterDistance Value;

      public UpdateAutoHideSizeData(DockItem dockItem, SplitterDistance value)
      {
        DockItem = dockItem;
        Value = value;
      }
    }
  }
}
