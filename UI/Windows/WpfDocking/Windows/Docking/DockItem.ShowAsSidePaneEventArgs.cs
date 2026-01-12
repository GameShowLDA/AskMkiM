using System.Windows.Controls;

namespace UI.Windows.WpfDocking.Windows.Docking
{
  public partial class DockItem
  {
    private sealed class ShowAsSidePaneEventArgs : ShowActionEventArgs<ShowAsSidePaneAction>
    {
      public ShowAsSidePaneEventArgs(DockItem dockItem, DockPaneNode paneNode, bool isAutoHide, Dock side, SplitterDistance size, bool isSizeForTarget, DockItemShowMethod showMethod)
          : base(dockItem, paneNode.DockControl, showMethod)
      {
        ShowAsSidePaneAction showAction = StrongTypeShowAction;
        DockPaneNodeStruct paneNodeStruct = DockPaneNodeStruct.FromDockPaneNode(paneNode);
        showAction.Target = paneNodeStruct.ItemIndex;
        showAction.IsFloating = paneNodeStruct.IsFloating;
        showAction.AncestorLevel = paneNodeStruct.AncestorLevel;
        showAction.IsAutoHide = isAutoHide;
        showAction.Side = side;
        showAction.Size = size;
        showAction.IsSizeForTarget = isSizeForTarget;
      }

      public override DockItemStateChangeMethod StateChangeMethod
      {
        get { return DockItemStateChangeMethod.ShowAsSidePane; }
      }
    }
  }
}
