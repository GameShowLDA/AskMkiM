namespace UI.Windows.WpfDocking.Windows.Docking
{
  public partial class DockItem
  {
    private sealed class ShowAsDockPositionEventArgs : ShowActionEventArgs<ShowAsDockPositionAction>
    {
      public ShowAsDockPositionEventArgs(DockItem dockItem, DockControl dockControl, DockPosition dockPosition, DockItemShowMethod showMethod)
          : base(dockItem, dockControl, showMethod)
      {
        StrongTypeShowAction.DockPosition = dockPosition;
      }

      public override DockItemStateChangeMethod StateChangeMethod
      {
        get { return DockItemStateChangeMethod.ShowAsDockPosition; }
      }
    }
  }
}
