using System.IO;
using System.Windows;
using System.Windows.Media;
using UI.Components.Invoke;
using UI.Controls.TextEditor;
using static Utilities.LoggerUtility;
using UserControl = System.Windows.Controls.UserControl;

namespace UI.Components.MultiEditorMethods
{
  public class FileManager
  {

    public List<OpenFileButton> OpenPages { get; set; }
    public List<UserControl> UserControls { get; set; }
    public Dictionary<string, string> FilePaths { get; set; }
    private readonly IFileManagerControl multiEditorControl;

    public FileManager(IFileManagerControl control)
    {
      multiEditorControl = control;
    }

    public void OpenFile(string path)
    {
      var nameFile = GetNameFile(path);
      if (string.IsNullOrEmpty(nameFile))
      {
        MessageBox.Show("Ошибка", "Ошибка при открытии файла");
        LogError($"Ошибка при открытии файла {path}");
        return;
      }

      try
      {
        string fileContent = System.IO.File.ReadAllText(path);

        var textEditor = new TextEditorUI();
        textEditor.Text = fileContent;

        var controlManager = new ControlManager(OpenPages, UserControls, FilePaths, multiEditorControl as MultiEditorControl);
        controlManager.AddControl(nameFile, textEditor);
        if (!FilePaths.ContainsKey(nameFile))
        {
          FilePaths.Add(nameFile, path);
        }
      }
      catch (Exception ex)
      {
        MessageBox.Show($"Ошибка при чтении файла: {ex.Message}", "Ошибка");
        LogError($"Ошибка при чтении файла: {ex.Message}");
      }
    }

    /// <summary>
    /// Создаёт новый файл.
    /// </summary>
    public void CreateNewFile()
    {
      var controlName = "Новый";
      var counter = 0;
      while (FilePaths.ContainsKey(controlName))
      {
        counter++;
        if (controlName != "Новый")
        {
          controlName = controlName.Remove(controlName.Length - (counter - 1).ToString().Length, (counter - 1).ToString().Length);
        }
        controlName += $"{counter}";
      }
      var textEditor = new TextEditorUI();
        var controlManager = new ControlManager(OpenPages, UserControls, FilePaths, multiEditorControl as MultiEditorControl);
      controlManager.AddControl(controlName, textEditor /*{ Text  = "Новый файл"}*/);
      FilePaths.Add(controlName, string.Empty);
    }

    /// <summary>
    /// Получает имя файла по пути к файлу.
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    private string GetNameFile(string path)
    {
      if (string.IsNullOrEmpty(path))
      {
        return string.Empty;
      }
      try
      {
        return System.IO.Path.GetFileName(path).ToString();
      }
      catch (Exception ex)
      {
        return string.Empty;
      }
    }

    public bool CompareFiles(OpenFileButton openPage)
    {
      var activeTab = OpenPages.FirstOrDefault(page => page.Background == (Brush)Application.Current.Resources["ActiveBorderSolidColorBrush"]);
      if (activeTab != null)
      {
        var fileName = activeTab.Text;
        if (FilePaths[fileName] == string.Empty)
        {
          return true;
        }
        else
        {
          var filePath = FilePaths[fileName];
          var content = File.ReadAllText(filePath);
          int index = OpenPages.IndexOf(activeTab);

          if (UserControls[index] is TextEditorUI)
          {
            var textEditor = UserControls[index] as TextEditorUI;
            return content != textEditor.Text;
          }
          return false;
        }
      }
      return false;
    }

    public FileManager(MultiEditorControl multiEditorControl)
    {
      this.FilePaths = new Dictionary<string, string>();
      this.UserControls = new List<UserControl>();
      this.OpenPages = new List<OpenFileButton>();
      this.multiEditorControl = multiEditorControl;
    }
  }

}
