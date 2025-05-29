using System.Windows;

namespace TestManyWindows
{
  public class DockingWindow : Window
  {
    public DockingPanel DockingPanel { get; private set; }

    public DockingWindow()
    {
      Width = 600;
      Height = 400;
      Title = "Отдельное окно";

      DockingPanel = new DockingPanel();
      Content = DockingPanel;

      DockingPanel.TabRemoved += CheckClose;
    }

    private void CheckClose()
    {
      if (!DockingPanel.HasTabs())
        Close();
    }

  }
}
