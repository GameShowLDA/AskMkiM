namespace UI.Windows.WpfDocking.Windows.Docking
{
  internal interface ICommand
  {
    void Execute(DockControl dockControl);
    void UnExecute(DockControl dockControl);
  }
}
