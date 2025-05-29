using System.Windows;
using UI.Controls.TextEditor;

namespace TestManyWindows
{
  public partial class MainWindow : Window
  {
    public DockingPanel DockingPanel { get; private set; }

    public MainWindow()
    {
      InitializeComponent();

      DockingPanel = new DockingPanel();
      Content = DockingPanel;

      AddNewTab("Документ 1");
      AddNewTab("Документ 2");
    }

    private void NewFile_Click(object sender, RoutedEventArgs e)
    {
      AddNewTab($"Документ {DockingPanel.TabCount + 1}");
    }

    private void AddNewTab(string title)
    {
      var editor = new TextEditorUI(); // Твой редактор
      DockingPanel.AddTab(title, editor);
    }

    public void ReceiveTab(string title, UIElement content)
    {
      DockingPanel.AddTab(title, content);
    }
  }

}
