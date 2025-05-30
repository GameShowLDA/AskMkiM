using Microsoft.Win32;
using System.IO;
using System.Windows;
using UI.Controls.TextEditor;

namespace TestDocking
{
  public partial class MainWindow : Window
  {
    private int _documentCounter = 1;

    public MainWindow()
    {
      InitializeComponent();
    }

    private void OpenFile_Click(object sender, RoutedEventArgs e)
    {
      var dialog = new OpenFileDialog
      {
        Filter = "Текстовые файлы (*.txt)|*.txt|Все файлы (*.*)|*.*"
      };

      if (dialog.ShowDialog() == true)
      {
        string filePath = dialog.FileName;
        string fileContent = File.ReadAllText(filePath);

        var textEditor = new TextEditorUI();
        textEditor.Text = File.ReadAllText(filePath);

        var dockItem = new DevZest.Windows.Docking.DockItem
        {
          Title = Path.GetFileName(filePath),
          TabText = Path.GetFileName(filePath),
          Content = textEditor
        };

        dockItem.Show(DockManager, DevZest.Windows.Docking.DockPosition.Document);
      }
    }

  }
}
