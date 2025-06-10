using ControlCommandAnalyser.Parsing;
using DevZest.Windows.Docking;
using DevZest.Windows.Docking.Primitives;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using UI.Components.Invoke;
using UI.Controls.TextEditor;
using static Utilities.LoggerUtility;
using Path = System.IO.Path;
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
        string fileContent = string.Empty;
        fileContent = GetFileContent(path, nameFile, fileContent);
        TextEditorContainer textEditorContainer = GetTextEditorContainer();
        var textEditor = CreateTextEditor(fileContent);

        if (Path.GetExtension(path).Equals(".pk", StringComparison.OrdinalIgnoreCase))
        {
          PkFileEncoding(fileContent, textEditor);
        }

        ManageFilename(path, nameFile, textEditorContainer, textEditor);
        ShowControl(textEditorContainer);
      }
      catch (Exception ex)
      {
        MessageBox.Show($"Ошибка при чтении файла: {ex.Message}", "Ошибка");
        LogException($"Ошибка при чтении файла", ex);
      }
    }

    private void ShowControl(TextEditorContainer textEditorContainer)
    {
      var controlManager = new ControlManager(OpenPages, UserControls, FilePaths, multiEditorControl);
      var tabButton = new OpenFileButton();
      tabButton.Header.Text = "Текстовый редактор";
      controlManager.ShowControl(textEditorContainer, tabButton);
    }

    public TextEditorContainer GetTextEditorContainer()
    {
      TextEditorContainer textEditorContainer = TextEditorContainerExists();
      if (textEditorContainer == null)
      {
        textEditorContainer = new TextEditorContainer();
        AddFileToControlManager("Текстовый редактор", textEditorContainer);
      }

      return textEditorContainer;
    }

    private void ManageFilename(string path, string nameFile, TextEditorContainer textEditorContainer, TextEditorUI textEditor)
    {
      if (!FilePaths.ContainsKey(nameFile))
      {
        FilePaths.Add(nameFile, path);
        ShowNewDockItem(nameFile, textEditorContainer, textEditor);
      }
      else
      {
        var fileWithSameNamePath = FilePaths.FirstOrDefault(file => file.Key == nameFile);
        if (fileWithSameNamePath.Value != path)
        {
          nameFile = GetDifferenceBetweenPaths(fileWithSameNamePath.Value, path);
          FilePaths.Add(nameFile, path);
          ShowNewDockItem(nameFile, textEditorContainer, textEditor);
        }
        else
        {
          var dockItem = textEditorContainer.DockManager.DockItems.FirstOrDefault(item => item.TabText == nameFile);
          ShowDockItem(textEditorContainer, dockItem);
        }
      }
    }

    private static void PkFileEncoding(string fileContent, TextEditorUI textEditor)
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

    private async void ShowNewDockItem(string nameFile, TextEditorContainer textEditorContainer, TextEditorUI textEditor)
    {
      var dockItem = new DockItem
      {
        Title = nameFile,
        TabText = nameFile,
        Content = textEditor
      };

      dockItem.CloseItem += (sender) =>
      {
        if (dockItem.Content is TextEditorUI textEditor)
        {
          if (textEditorContainer != null)
          {
            var controlManager = new ControlManager(OpenPages, UserControls, FilePaths, multiEditorControl);
            var foundPage = OpenPages.FirstOrDefault(page => page.Text == "Текстовый редактор");
            controlManager.RemoveControl(foundPage, textEditor);
            FilePaths.Remove(dockItem.TabText);
            if (FilePaths.Count == 0)
            {
              RemoveTextEditorContainer(textEditorContainer);
            }
          }
        }
      };

      await Task.Delay(1).ConfigureAwait(true);

      ShowDockItem(textEditorContainer, dockItem);
    }

    private void RemoveTextEditorContainer(TextEditorContainer textEditorContainer)
    {
      var controlManager = new ControlManager(OpenPages, UserControls, FilePaths, multiEditorControl);
      var foundPage = OpenPages.FirstOrDefault(page => page.Text == "Текстовый редактор");
      controlManager.RemoveControl(foundPage, textEditorContainer);
    }

    public void ShowDockItem(TextEditorContainer textEditorContainer, DockItem dockItem)
    {
      try
      {
        if (!textEditorContainer.DockManager.IsLoaded)
        {
          var capturedDockItem = dockItem;
          textEditorContainer.DockManager.Loaded += (s, e) =>
          {
            capturedDockItem.Show(textEditorContainer.DockManager, DockPosition.Document);
          };
        }
        else
        {
          dockItem.Show(textEditorContainer.DockManager, DockPosition.Document);
        }
      }
      catch (Exception ex)
      {
        LogError($"Ошибка при открытии документа: {ex}");
      }
    }


    private TextEditorContainer TextEditorContainerExists()
    {
      var container = UserControls.FirstOrDefault(textEditorContainer => textEditorContainer.GetType() == typeof(TextEditorContainer));
      return container as TextEditorContainer;
    }

    private string GetFileContent(string path, string nameFile, string fileContent)
    {
      Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
      if (nameFile.ToLower().Contains(".pk") && !nameFile.ToLower().Contains(".pkw"))
      {
        var content = new List<string>();
        foreach (string line in File.ReadLines(path, Encoding.GetEncoding(866)))
        {
          if (!string.IsNullOrEmpty(line))
          {
            content.Add(line);
          }
        }
        if (content.Count > 0)
        {
          fileContent = string.Join("\n", content);
        }
      }
      else
      {
        fileContent = ReadFileContent(path);
      }

      return fileContent;
    }

    public string GetDifferenceBetweenPaths(string existingPath, string newPath)
    {
      var existingParts = existingPath.Split(Path.DirectorySeparatorChar);
      var newParts = newPath.Split(Path.DirectorySeparatorChar);

      int minLength = Math.Min(existingParts.Length, newParts.Length);
      int commonLength = 0;

      // Находим индекс, где пути перестают совпадать
      for (int i = 0; i < minLength; i++)
      {
        if (!string.Equals(existingParts[i], newParts[i], StringComparison.OrdinalIgnoreCase))
          break;

        commonLength++;
      }

      // Гарантируем, что хотя бы одна дополнительная папка будет в ключе
      int startIndex = Math.Max(0, newParts.Length - 2); // минимум: папка + файл

      // Но если всё отличается, берём всю вторую часть после общего пути
      if (commonLength < newParts.Length - 1)
        startIndex = commonLength;

      return string.Join(Path.DirectorySeparatorChar.ToString(), newParts.Skip(startIndex));
    }



    /// <summary>
    /// Добавляет контрол в мультиэдитор.
    /// </summary>
    /// <param name="nameFile">Имя добавляемого файла.</param>
    /// <param name="textEditor">Экземпляр класса TextEditorUI.</param>
    private void AddFileToControlManager(string nameFile, TextEditorContainer textEditorContainer)
    {
      var controlManager = new ControlManager(OpenPages, UserControls, FilePaths, multiEditorControl);
      controlManager.AddControl(nameFile, textEditorContainer, OpenFileButton.TypeWindow.Files);
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
      TextEditorContainer textEditorContainer = GetTextEditorContainer();

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
      ShowNewDockItem(controlName, textEditorContainer, textEditor);
      //var controlManager = new ControlManager(OpenPages, UserControls, FilePaths, multiEditorControl as MultiEditorControl);
      //controlManager.AddControl(controlName, textEditor, OpenFileButton.TypeWindow.Files /*{ Text  = "Новый файл"}*/);
      FilePaths.Add(controlName, string.Empty);
    }

    /// <summary>
    /// Сравнивает содержимое открытого файла с текстом в текущем редакторе.
    /// </summary>
    /// <param name="openPage">Объект кнопки, представляющий открытую страницу.</param>
    /// <returns>Возвращает <c>true</c>, если файл не сохранен (содержимое изменилось), <c>false</c> в противном случае.</returns>
    public bool CompareFiles(DockItem control)
    {
      //var activeTab = OpenPages.FirstOrDefault(page => page.Background == (Brush)Application.Current.Resources["ActiveBorderSolidColorBrush"]);
      //if (activeTab != null)
      //{
      var fileName = control.Title;
      //int index = OpenPages.IndexOf(activeTab);
      if (fileName.Contains(".opk"))
      {
        return false;
      }
      if (FilePaths[fileName] == string.Empty)
      {
        if (control.Content is TextEditorUI)
        {
          var textEditor = control.Content as TextEditorUI;
          return !string.IsNullOrEmpty(textEditor.Text) && !string.IsNullOrWhiteSpace(textEditor.Text);
        }

        return false;
      }
      else
      {
        var filePath = FilePaths[fileName];
        if (File.Exists(filePath))
        {

          var content = File.ReadAllText(filePath);

          if (control.Content is TextEditorUI)
          {
            var textEditor = control.Content as TextEditorUI;
            return content != textEditor.Text;
          }

          return false;
        }
        else
        {
          MessageBox.Show("Файл был удален или поврежден", "Ошибка!", MessageBoxButton.OK, MessageBoxImage.Error);
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
