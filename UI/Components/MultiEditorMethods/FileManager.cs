using AppConfiguration.Base;
using DevZest.Windows.Docking;
using ICSharpCode.AvalonEdit;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Shapes;
using UI.Components.FileComparerControls;
using UI.Components.Invoke;
using UI.Controls;
using UI.Controls.TextEditor;
using static UI.Components.Invoke.OpenFileButton;
using static UI.Controls.TextEditor.TextEditorUI;
using static Utilities.LoggerUtility;
using Path = System.IO.Path;
using UserControl = System.Windows.Controls.UserControl;
using Ude;

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
        var fileData = GetFileContent(path, nameFile, fileContent).ToTuple();
        fileContent = fileData.Item1;
        var encoding = fileData.Item2;
        TextEditorContainer textEditorContainer = GetContainer(EditorType.TextEditor);
        if (textEditorContainer == null)
        {
          textEditorContainer = CreateContainer(EditorType.TextEditor);
        }

        var fileType = GetFileType(nameFile);
        var newFileName = ManageFilename(path, nameFile);

        var textEditorModel = new TextEditorModel(path, newFileName, encoding);
        var textEditor = CreateTextEditor(textEditorModel, fileContent, fileType);
        EventAggregator.RaiseTextEditorActivated(textEditor);

        ShowNewDockItem(newFileName, textEditorContainer, textEditor);

        ShowControl(textEditorContainer, EditorType.TextEditor);
      }
      catch (Exception ex)
      {
        MessageBox.Show($"Ошибка при чтении файла: {ex.Message}", "Ошибка");
        LogException($"Ошибка при чтении файла", ex);
      }
    }

    public TextEditorContainer CreateContainer(EditorType editorType)
    {
      var textEditorContainer = new TextEditorContainer();
      AddFileToControlManager(editorType.ToString(), textEditorContainer);
      if (editorType == EditorType.TextEditor)
      {
        
      }

      return textEditorContainer;
    }

    public TextEditorUI CreateTranslationFileAsync()
    {
      string fileName = $"Трансляция_{DateTime.Now:HHmmss}.opkw";
      var textEditorModel = new TextEditorModel(fileName);

      var textEditor = new TextEditorUI(TextEditorUI.FileType.OPKW, textEditorModel)
      {
        Text = "// Результат трансляции появится здесь...",
        IsReadOnly = true
      };

      return textEditor;
    }

    public TextEditorUI GetActiveTextEditor()
    {
      TextEditorContainer textEditorContainer = GetContainer(EditorType.TextEditor);
      if (textEditorContainer == null)
      {
        return null;
      }
      else
      {
        return textEditorContainer.GetTextEditor();
      }
    }

    public bool RemoveActiveTextEditor(bool isTranslation)
    {
      TextEditorContainer textEditorContainer = GetContainer(EditorType.TextEditor);
      var foundDockItem = textEditorContainer.DockManager.DockItems.FirstOrDefault(item => item.IsActiveItem == true);
      if (foundDockItem != null && foundDockItem.Content is TextEditorUI textEditor)
      {
        var controlManager = new ControlManager(OpenPages, UserControls, FilePaths, multiEditorControl);
        var foundPage = OpenPages.FirstOrDefault(page => page.Text == EditorType.TextEditor.ToString());
        controlManager.RemoveControl(foundPage, textEditor, isTranslation);
        FilePaths.Remove(foundDockItem.TabText);

        bool closed = foundDockItem.Close();

        if (textEditorContainer.DockManager.DockItems.Count == 0)
        {
          RemoveTextEditorContainer(textEditorContainer);
        }

        return closed;
      }
      return false;
    }


    private void ShowControl(TextEditorContainer textEditorContainer, EditorType editorType)
    {
      var controlManager = new ControlManager(OpenPages, UserControls, FilePaths, multiEditorControl);
      var tabButton = new OpenFileButton();
      tabButton.Header.Text = editorType.ToString();
      controlManager.ShowControl(textEditorContainer, tabButton);
    }

    public TextEditorContainer GetContainer(EditorType editorType)
    {
      var containerPage = OpenPages.FirstOrDefault(page => page.Text == editorType.DisplayName);
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

    private string ManageFilename(string path, string nameFile)
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
    internal async void ShowNewDockItem(string nameFile, TextEditorContainer textEditorContainer, UserControl textEditor)
    {
      var dockItem = new DockItem
      {
        Title = nameFile,
        TabText = nameFile,
        Content = textEditor
      };

      dockItem.CloseItem += (sender) =>
      {
        if (dockItem.Content is TextEditorUI || dockItem.Content is FileCompareControl)
        {
          if (textEditorContainer != null)
          {
            var controlManager = new ControlManager(OpenPages, UserControls, FilePaths, multiEditorControl);
            var foundPage = OpenPages.FirstOrDefault(page => page.Text == EditorType.TextEditor.ToString());
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

    private async Task<TranslatorItem> ShowNewDockItem(string nameFile, TextEditorContainer textEditorContainer, TextEditorUI textEditor, TextEditorUI translatorEditor)
    {
      try
      {

        var translatorItem = new TranslatorItem();
        translatorItem.SetLeftEditor(textEditor);
        translatorItem.SetRightEditor(translatorEditor);
        translatorItem.SetRightEditorName(translatorEditor.TextEditorModel.FileName);
        translatorItem.SetLeftEditorName(textEditor.TextEditorModel.FileName);
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
              var foundPage = OpenPages.FirstOrDefault(page => page.Text == EditorType.TextEditor.ToString());
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

        return translatorItem;
      }
      catch (Exception ex)
      {
        MessageBox.Show($"Системная ошибка: {ex}", "Ошибка!", MessageBoxButton.OK, MessageBoxImage.Error);
        LogError($"Системная ошибка: {ex}");
        return null;
      }
    }

    private void RemoveTextEditorContainer(TextEditorContainer textEditorContainer)
    {
      var controlManager = new ControlManager(OpenPages, UserControls, FilePaths, multiEditorControl);
      var foundPage = OpenPages.FirstOrDefault(page => page.Text == EditorType.TextEditor.ToString());
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

    private Encoding DetectEncoding(string filePath)
    {
      using var fileStream = File.OpenRead(filePath);
      var detector = new CharsetDetector();
      detector.Feed(fileStream);
      detector.DataEnd();

      if (detector.Charset != null)
      {
        try
        {
          return Encoding.GetEncoding(detector.Charset);
        }
        catch
        {
          // Fallback в случае недоподдерживаемой кодировки
          return Encoding.UTF8;
        }
      }

      // Если определить не удалось — используем по умолчанию
      return Encoding.UTF8;
    }

    private (string, Encoding) GetFileContent(string path, string nameFile, string fileContent)
    {
      Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

      Encoding encoding = DetectEncoding(path);

      var content = new List<string>();
      foreach (string line in File.ReadLines(path, encoding))
      {
        if (!string.IsNullOrEmpty(line))
        {
          content.Add(line);
        }
      }

      fileContent = content.Count > 0 ? string.Join("\n", content) : string.Empty;

      return (fileContent, encoding);
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
    private TextEditorUI CreateTextEditor(TextEditorModel textEditorModel, string fileContent, FileType fileType = FileType.None)
    {
      var textEditor = new TextEditorUI(fileType, textEditorModel);
      textEditor.Text = fileContent;
      //EventAggregator.RaiseTextEditorActivated(control);
      return textEditor;
    }

    /// <summary>
    /// Создаёт новый файл.
    /// </summary>
    public void CreateNewFile()
    {
      TextEditorContainer textEditorContainer = GetContainer(EditorType.TextEditor);

      if (textEditorContainer == null)
      {
        textEditorContainer = CreateContainer(EditorType.TextEditor);
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

    internal async Task<TranslatorItem> AddTranslatorItem(TextEditorUI editor, TextEditorUI translateEditor, EditorType editorType)
    {
      try
      {
        TextEditorContainer textEditorContainer = GetContainer(editorType);
        if (textEditorContainer == null)
        {
          textEditorContainer = CreateContainer(editorType);
        }
        var item = await ShowNewDockItem($"Трансляция {editor.TextEditorModel.FileName}", textEditorContainer, editor, translateEditor);

        ShowControl(textEditorContainer, EditorType.Translator);
        return item;
      }
      catch (Exception ex)
      {
        MessageBox.Show($"Ошибка при чтении файла: {ex.Message}", "Ошибка");
        LogException($"Ошибка при чтении файла", ex);
        return null;
      }
    }

    internal void OpenFolder()
    {
      TextEditorContainer textEditorContainer = GetContainer(EditorType.TextEditor);
      if (textEditorContainer == null)
      {
        textEditorContainer = GetContainer(EditorType.Translator);
        if (textEditorContainer == null)
        {
          return;
        }
        else
        {
          var translatorEditor = textEditorContainer.DockManager.DockItems.FirstOrDefault(item => item.IsActiveItem == true);
          if (translatorEditor != null && translatorEditor.Content is TranslatorItem translator)
          {
            var leftEditor = translator.GetLeftEditor();
            OpenFileFolder(leftEditor.TextEditorModel.FilePath);
          }
        }
      }
      else
      {
        var activeTextEditor = textEditorContainer.GetTextEditor();
        if (activeTextEditor != null)
        {
          OpenFileFolder(activeTextEditor.TextEditorModel.FilePath);
        }
      }
    }

    private static void OpenFileFolder(string path)
    {
      string folder = Path.GetDirectoryName(path);
      if (!string.IsNullOrEmpty(folder) && Directory.Exists(folder))
      {
        Process.Start(new ProcessStartInfo
        {
          FileName = folder,
          UseShellExecute = true,
          Verb = "open"
        });
      }
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
