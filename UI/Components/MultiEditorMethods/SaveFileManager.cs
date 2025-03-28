using System.IO;
using System.Windows.Forms;
using System.Windows.Media;
using UI.Components.Invoke;
using UI.Controls.TextEditor;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;
using static Utilities.LoggerUtility;
using Path = System.IO.Path;
using System.Windows;

namespace UI.Components.MultiEditorMethods
{
  public class SaveFileManager
  {
    FileManager fileManager { get; set; }

    public void SaveFileDialog(ref MessageBoxResult result, ref bool saveFileResult, int index)
    {
      var needToSave = fileManager.CompareFiles(fileManager.OpenPages[index]);
      if (needToSave)
      {
        result = MessageBox.Show(
            $"Сохранить файл {fileManager.OpenPages[index].Text} перед закрытием?",
            "Подтверждение",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        if (result == MessageBoxResult.Yes)
        {
          saveFileResult = SaveFile(fileManager.OpenPages[index]);
        }
      }
    }


    public bool SaveFile(OpenFileButton activeTab)
    {
      if (activeTab != null)
      {
        var fileName = activeTab.Text;
        if (fileManager.FilePaths[fileName] == string.Empty)
        {
          return SaveFileAs();
        }
        else
        {
          var filePath = fileManager.FilePaths[fileName];
          return SaveDataFromTextEditor(activeTab, filePath);
        }
      }
      return false;
    }

    // TODO: добавить сохранение файлов при закрытии приложения
    public bool SaveFileAs()
    {
      using (SaveFileDialog saveFileDialog = new SaveFileDialog())
      {
        var activeTab = fileManager.OpenPages.FirstOrDefault(page => page.Background == (Brush)Application.Current.Resources["ActiveBorderSolidColorBrush"]);
        if (activeTab != null)
        {
          saveFileDialog.Filter = "Text Files (*.txt)|*.txt|RTF Files (*.rtf)|*.rtf";
          saveFileDialog.Title = "Сохранить файл как";
          saveFileDialog.FileName = activeTab.Text;
          saveFileDialog.FileName = Path.GetFileNameWithoutExtension(activeTab.Text);
          if (saveFileDialog.ShowDialog() == DialogResult.OK)
          {
            string filePath = saveFileDialog.FileName;
            SaveDataFromTextEditor(activeTab, filePath);
            RenamePage(activeTab, filePath);
            var fileName = Path.GetFileName(filePath);
            if (!fileManager.FilePaths.ContainsKey(fileName))
            {
              fileManager.FilePaths.Add(fileName, filePath);
            }
            else
            {
              fileManager.FilePaths[fileName] = filePath;
            }
            return true;
          }
          else
          {
            return false;
          }
        }
        return false;
      }
    }

    private bool SaveDataFromTextEditor(OpenFileButton activeTab, string filePath)
    {
      string fileData = string.Empty;

      int index = fileManager.OpenPages.IndexOf(activeTab);
      if (fileManager.UserControls[index] is TextEditorUI)
      {
        var textEditor = fileManager.UserControls[index] as TextEditorUI;
        fileData = textEditor.Text;
        File.WriteAllText(filePath, fileData);
        LogInformation($"Файл {filePath} сохранен");
        MessageBox.Show($"Файл {filePath} сохранен");
        return true;
      }
      return false;
    }

    private void RenamePage(OpenFileButton activeTab, string filePath)
    {
      var acivePage = fileManager.OpenPages.FirstOrDefault(p => p == activeTab);
      if (acivePage != null)
      {
        activeTab.Header.Text = System.IO.Path.GetFileName(filePath);
      }
    }

    public SaveFileManager(FileManager fileManager)
    {
      this.fileManager = fileManager;
    }
  }
}
