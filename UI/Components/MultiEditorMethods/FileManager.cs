using DevZest.Windows.Docking;
using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Shapes;
using UI.Components.Invoke;
using UI.Controls;
using UI.Controls.TextEditor;
using static UI.Components.Invoke.OpenFileButton;
using static UI.Controls.TextEditor.TextEditorUI;
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
        var pageName = "Текстовый редактор";
        TextEditorContainer textEditorContainer = GetContainer(pageName);
        if (textEditorContainer == null)
        {
          textEditorContainer = CreateContainer(pageName);
        }

        var fileType = GetFileType(nameFile);
        var textEditor = CreateTextEditor(fileContent, fileType);

        var newFileName = ManageFilename(path, nameFile, textEditorContainer, textEditor);
        ShowNewDockItem(nameFile, textEditorContainer, textEditor);

        ShowControl(textEditorContainer);
      }
      catch (Exception ex)
      {
        MessageBox.Show($"Ошибка при чтении файла: {ex.Message}", "Ошибка");
        LogException($"Ошибка при чтении файла", ex);
      }
    }

    public TextEditorContainer CreateContainer(string pageName)
    {
      var textEditorContainer = new TextEditorContainer();
      AddFileToControlManager(pageName, textEditorContainer);
      return textEditorContainer;
    }

    public TextEditorUI CreateTranslationFileAsync()
    {
      var pageName = "Текстовый редактор";
      TextEditorContainer textEditorContainer = GetContainer(pageName);
      if (textEditorContainer == null)
      {
        textEditorContainer = CreateContainer(pageName);
      }

      string fileName = $"Трансляция_{DateTime.Now:HHmmss}.opkw";

      var textEditor = new TextEditorUI(TextEditorUI.FileType.OPKW)
      {
        Text = "// Результат трансляции появится здесь...",
        IsReadOnly = true
      };

      ManageFilename(fileName, fileName, textEditorContainer, textEditor);

      return textEditor;
    }

    public TextEditorUI GetActiveTextEditor()
    {
      var pageName = "Текстовый редактор";
      TextEditorContainer textEditorContainer = GetContainer(pageName);
      if (textEditorContainer == null)
      {
        textEditorContainer = CreateContainer(pageName);
      }
      return textEditorContainer.GetTextEditor();
    }

    public bool RemoveActiveTextEditor()
    {
      TextEditorContainer textEditorContainer = GetContainer("Текстовый редактор");
      return textEditorContainer.RemoveActiveTextEditor(textEditorContainer);
    }

    private void ShowControl(TextEditorContainer textEditorContainer)
    {
      var controlManager = new ControlManager(OpenPages, UserControls, FilePaths, multiEditorControl);
      var tabButton = new OpenFileButton();
      tabButton.Header.Text = "Текстовый редактор";
      controlManager.ShowControl(textEditorContainer, tabButton);
    }

    public TextEditorContainer GetContainer(string pageText)
    {
      var containerPage = OpenPages.FirstOrDefault(page => page.Text == pageText);
      if (containerPage == null)
      {
        return null;
      }
      else
      {
        var foundElement = UserControls[OpenPages.IndexOf(containerPage)];
        if (foundElement != null && foundElement is TextEditorContainer)
        {
          TextEditorContainer textEditorContainer = foundElement as TextEditorContainer;
          return textEditorContainer;
        }
        else
        {
          return null;
        }
      }
    }

    private string ManageFilename(string path, string nameFile, TextEditorContainer textEditorContainer, TextEditorUI textEditor)
    {
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
        else
        {
          var dockItem = textEditorContainer.DockManager.DockItems.FirstOrDefault(item => item.TabText == nameFile);
        }
      }
      return nameFile;
    }

    /// <summary>
    /// Обрабатывает добавление или открытие файла с учётом уже открытых файлов в редакторе.
    /// При необходимости добавляет новый DockItem или показывает существующий.
    /// </summary>
    /// <param name="path">Полный путь к файлу.</param>
    /// <param name="nameFile">Имя файла.</param>
    /// <param name="textEditorContainer">Контейнер редактора, в котором будут размещаться DockItem'ы.</param>
    /// <param name="textEditor">Экземпляр редактора для отображения содержимого файла.</param>
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

    private async void ShowNewDockItem(string nameFile, TextEditorContainer textEditorContainer, TextEditorUI textEditor, TextEditorUI translatorEditor)
    {
      var translatorItem = new TranslatorItem();
      translatorItem.SetLeftEditor(textEditor);
      translatorItem.SetRightEditor(translatorEditor);
      var dockItem = new DockItem
      {
        Title = nameFile,
        TabText = nameFile,
        Content = translatorItem
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
    private void AddFileToControlManager(string nameFile, UserControl container)
    {
      var controlManager = new ControlManager(OpenPages, UserControls, FilePaths, multiEditorControl);
      controlManager.AddControl(nameFile, container, OpenFileButton.TypeWindow.Files);
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
    private TextEditorUI CreateTextEditor(string fileContent, FileType fileType = FileType.None)
    {
      var textEditor = new TextEditorUI(fileType);
      textEditor.Text = fileContent;
      return textEditor;
    }

    /// <summary>
    /// Создаёт новый файл.
    /// </summary>
    public void CreateNewFile()
    {
      var pageName = "Текстовый редактор";
      TextEditorContainer textEditorContainer = GetContainer(pageName);

      if (textEditorContainer == null)
      {
        textEditorContainer = CreateContainer(pageName);
      }

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
      FilePaths.Add(controlName, string.Empty);
    }

    /// <summary>
    /// Сравнивает содержимое открытого файла с текстом в текущем редакторе.
    /// </summary>
    /// <param name="openPage">Объект кнопки, представляющий открытую страницу.</param>
    /// <returns>Возвращает <c>true</c>, если файл не сохранен (содержимое изменилось), <c>false</c> в противном случае.</returns>
    public bool CompareFiles(DockItem control)
    {
      var fileName = control.Title;
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

    private FileType GetFileType(string fileName)
    {
      if (string.IsNullOrEmpty(fileName))
        return FileType.None;
      var ext = Path.GetExtension(fileName).ToLowerInvariant();
      return ext switch
      {
        ".pk" => FileType.PK,
        ".pkw" => FileType.PKW,
        ".opk" => FileType.OPK,
        ".opkw" => FileType.OPKW,
        _ => FileType.None
      };
    }

    internal void AddTranslatorItem(TextEditorUI editor, TextEditorUI translateEditor, string pageName)
    {
      try
      {
        TextEditorContainer textEditorContainer = GetContainer(pageName);
        if (textEditorContainer == null)
        {
          textEditorContainer = CreateContainer(pageName);
        }
        ShowNewDockItem("Заглушка", textEditorContainer, editor, translateEditor);

        ShowControl(textEditorContainer);
      }
      catch (Exception ex)
      {
        MessageBox.Show($"Ошибка при чтении файла: {ex.Message}", "Ошибка");
        LogException($"Ошибка при чтении файла", ex);
      }
    }
  }
}
