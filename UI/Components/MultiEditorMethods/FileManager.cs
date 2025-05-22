using System.IO;
using System.Windows;
using System.Windows.Media;
using ControlCommandAnalyser.Parsing;
using UI.Components.Invoke;
using UI.Controls.TextEditor;
using static Utilities.LoggerUtility;
using UserControl = System.Windows.Controls.UserControl;

namespace UI.Components.MultiEditorMethods
{
  /// <summary>
  /// Класс для работы с файлами.
  /// </summary>
  public class FileManager
  {
    /// <summary>
    /// Получает или задает список объектов кнопки, представляющей открытую страницу. 
    /// </summary>
    public List<OpenFileButton> OpenPages { get; set; }

    /// <summary>
    /// Получает или задает список пользовательских контролов, которые отображаются в приложении.
    /// </summary>
    public List<UserControl> UserControls { get; set; }

    /// <summary>
    /// Получает или задает словарь, где имя файла - ключ, а путь к файлу - значение.
    /// </summary>
    public Dictionary<string, string> FilePaths { get; set; }

    /// <summary>
    /// Интерфейс для создания связи между файловым мененджером и Multi Editor Control.
    /// </summary>
    private readonly MultiEditorControl multiEditorControl;

    /// <summary>
    /// Открывает файл, который находится по заданному пути, в текстовом редакторе.
    /// </summary>
    /// <param name="path">Путь к файлу.</param>
    public void OpenFile(string path)
    {
      var nameFile = GetNameFile(path);
      if (string.IsNullOrEmpty(nameFile))
      {
        MessageBox.Show("Ошибка при открытии файла", $"Ошибка при открытии файла {path}");
        return;
      }

      try
      {
        string fileContent = ReadFileContent(path);

        var textEditor = CreateTextEditor(fileContent);

        if (Path.GetExtension(path).Equals(".pk", StringComparison.OrdinalIgnoreCase))
        {
          var lines = fileContent.Split('\n');

          var recognizer = new LineRecognizer();
          var parsed = lines.Select((line, index) => recognizer.Parse(line, index)).ToList();

          var highlighter = new SyntaxHighlightPlanner();
          var highlights = highlighter.Build(parsed);

          if (textEditor is TextEditorUI editorUI)
          {
            editorUI.ApplyHighlighting(highlights);
          }
        }

        if (!FilePaths.ContainsKey(nameFile))
        {
          FilePaths.Add(nameFile, path);
        }
        else
        {
          var fileWithSameNamePath = FilePaths.FirstOrDefault(file => file.Key == nameFile);
          if (fileWithSameNamePath.Value != path)
          {
            nameFile = GetDifferenceBetweenPaths(fileWithSameNamePath.Value, path);
            FilePaths.Add(nameFile, path);
          }
        }
        
        AddFileToControlManager(nameFile, textEditor);
      }
      catch (Exception ex)
      {
        MessageBox.Show($"Ошибка при чтении файла: {ex.Message}", "Ошибка");
        LogException($"Ошибка при чтении файла", ex);
      }
    }

    public string GetDifferenceBetweenPaths(string path1, string path2)
    {
      // Ищем точку различия
      var path1Parts = path1.Split(Path.DirectorySeparatorChar);
      var path2Parts = path2.Split(Path.DirectorySeparatorChar);

      int minLength = Math.Min(path1Parts.Length, path2Parts.Length);
      int diffIndex = 0;

      // Ищем точку, где пути начинают различаться
      for (int i = 0; i < minLength; i++)
      {
        if (path1Parts[i] != path2Parts[i])
        {
          diffIndex = i;
          break;
        }
      }

      // Возвращаем оставшуюся часть пути после различия
      return string.Join(Path.DirectorySeparatorChar.ToString(), path1Parts.Skip(diffIndex));
    }

    /// <summary>
    /// Добавляет контрол в мультиэдитор.
    /// </summary>
    /// <param name="nameFile">Имя добавляемого файла.</param>
    /// <param name="textEditor">Экземпляр класса TextEditorUI.</param>
    private void AddFileToControlManager(string nameFile, TextEditorUI textEditor)
    {
      var controlManager = new ControlManager(OpenPages, UserControls, FilePaths, multiEditorControl);
      controlManager.AddControl(nameFile, textEditor, OpenFileButton.TypeWindow.Files);
    }

    /// <summary>
    /// Получает имя файла по его пути.
    /// </summary>
    /// <param name="path">Путь к файлу.</param>
    /// <returns>Имя файла или пустую строку, если имя не найдено.</returns>
    private string GetNameFile(string path)
    {
      return string.IsNullOrEmpty(path) ? string.Empty : System.IO.Path.GetFileName(path);
    }

    /// <summary>
    /// Читает содержимое файла по указанному пути.
    /// </summary>
    /// <param name="path">Путь к файлу.</param>
    /// <returns>Содержимое файла в виде строки.</returns>
    private string ReadFileContent(string path)
    {
      return System.IO.File.ReadAllText(path);
    }

    /// <summary>
    /// Создает новый экземпляр <see cref="TextEditorUI"/> и устанавливает его текст.
    /// </summary>
    /// <param name="fileContent">Содержимое файла, которое будет установлено в редактор.</param>
    /// <returns>Новый экземпляр <see cref="TextEditorUI"/>.</returns>
    private TextEditorUI CreateTextEditor(string fileContent)
    {
      var textEditor = new TextEditorUI();
      textEditor.Text = fileContent;
      return textEditor;
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
      controlManager.AddControl(controlName, textEditor, OpenFileButton.TypeWindow.Files /*{ Text  = "Новый файл"}*/);
      FilePaths.Add(controlName, string.Empty);
    }

    /// <summary>
    /// Сравнивает содержимое открытого файла с текстом в текущем редакторе.
    /// </summary>
    /// <param name="openPage">Объект кнопки, представляющий открытую страницу.</param>
    /// <returns>Возвращает <c>true</c>, если файл не сохранен (содержимое изменилось), <c>false</c> в противном случае.</returns>
    public bool CompareFiles(OpenFileButton openPage)
    {
      var activeTab = OpenPages.FirstOrDefault(page => page.Background == (Brush)Application.Current.Resources["ActiveBorderSolidColorBrush"]);
      if (activeTab != null)
      {
        var fileName = activeTab.Text;
        int index = OpenPages.IndexOf(activeTab);
        if (fileName.Contains(".opk")) 
        {
          return false;
        }
        if (FilePaths[fileName] == string.Empty)
        {
          if (UserControls[index] is TextEditorUI)
          {
            var textEditor = UserControls[index] as TextEditorUI;
            return !string.IsNullOrEmpty(textEditor.Text) && !string.IsNullOrWhiteSpace(textEditor.Text);
          }

          return false;
        }
        else
        {
          var filePath = FilePaths[fileName];
          var content = File.ReadAllText(filePath);

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

    /// <summary>
    /// Конструктор для инициализации файлового менеджера.
    /// </summary>
    /// <param name="multiEditorControl">Экземпляр класса MultiEditorControl.</param>
    public FileManager(MultiEditorControl multiEditorControl)
    {
      this.FilePaths = new Dictionary<string, string>();
      this.UserControls = new List<UserControl>();
      this.OpenPages = new List<OpenFileButton>();
      this.multiEditorControl = multiEditorControl;
    }
  }
}
