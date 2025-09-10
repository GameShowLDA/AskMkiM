using AppConfiguration.Base;
using Message;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Ude;
using UI.Components.ArchiveControls;
using UI.Components.ArchiveManager.Models;
using UI.Components.FileComparerControls;
using UI.Components.Invoke;
using UI.Controls;
using UI.Controls.Runner;
using UI.Controls.TextEditor;
using UI.Windows.WpfDocking.Windows.Docking;
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
    public ObservableCollection<OpenFileButton> OpenPages { get; set; }

    /// <summary>
    /// Получает или задает список пользовательских контролов, которые отображаются в приложении.
    /// </summary>
    public ObservableCollection<UserControl> UserControls { get; set; }

    /// <summary>
    /// Получает или задает словарь, где имя файла - ключ, а путь к файлу - значение.
    /// </summary>
    public Dictionary<string, string> FilePaths { get; set; }

    /// <summary>
    /// Интерфейс для создания связи между файловым мененджером и Multi Editor Control.
    /// </summary>
    private readonly MultiEditorControl multiEditorControl;

    public int GetCountConrols()
    {
      return UserControls.Count;
    }

    /// <summary>
    /// Открывает файл, который находится по заданному пути, в текстовом редакторе.
    /// </summary>
    /// <param name="path">Путь к файлу.</param>
    public void OpenFile(string path)
    {
      var nameFile = GetNameFile(path);
      if (string.IsNullOrEmpty(nameFile))
      {
        MessageBoxCustom.Show("Ошибка при открытии файла", $"Ошибка при открытии файла {path}", image: MessageBoxImage.Error);
        return;
      }

      try
      {
        string fileContent = string.Empty;
        var fileData = GetFileContent(path).ToTuple();
        fileContent = fileData.Item1;
        var encoding = fileData.Item2;
        TextEditorContainer textEditorContainer = GetContainer(EditorType.TextEditor);
        if (textEditorContainer == null)
        {
          textEditorContainer = CreateContainer(EditorType.TextEditor);
        }

        var fileType = GetFileType(nameFile);
        if (FilePaths.ContainsValue(path))
        {
          var existingItem = textEditorContainer.DockManager.DockItems.FirstOrDefault(item => item.TabText == nameFile);
          if (existingItem != null)
          {
            ShowDockItem(textEditorContainer, existingItem);
            ShowControl(textEditorContainer, EditorType.TextEditor);
            return;
          }
        }
        var newFileName = ManageFilename(path, nameFile);

        var textEditorModel = new TextEditorModel(path, newFileName, encoding);
        var textEditor = CreateTextEditor(textEditorModel, fileContent, fileType);
        EventAggregator.RaiseTextEditorActivated(textEditor);

        ShowNewDockItem(newFileName, textEditorContainer, textEditor);

        ShowControl(textEditorContainer, EditorType.TextEditor);
      }
      catch (Exception ex)
      {
        MessageBoxCustom.Show($"Ошибка при чтении файла: {ex.Message}", "Ошибка", image: MessageBoxImage.Error);
        LogException($"Ошибка при чтении файла", ex);
      }
    }

    /// <summary>
    /// Создает контейнер для вкладок заданного типа.
    /// </summary>
    /// <param name="editorType">Тип вкладок.</param>
    /// <returns>Контейнер для вкладок заданного типа.</returns>
    public TextEditorContainer CreateContainer(EditorType editorType)
    {
      var textEditorContainer = new TextEditorContainer();
      AddFileToControlManager(editorType.ToString(), textEditorContainer);
      return textEditorContainer;
    }

    /// <summary>
    /// Создает текстовый редактор с результатами трансляции файла.
    /// </summary>
    /// <returns>Текстовый редактор с странслированным файлом.</returns>
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

    /// <summary>
    /// Получает активный текстовый редактор.
    /// </summary>
    /// <returns></returns>
    public TextEditorUI GetActiveTextEditor(EditorType editorType)
    {
      var activeTab = OpenPages.FirstOrDefault(page => page.Background == (Brush)Application.Current.Resources["ActiveBorderSolidColorBrush"]);
      if (activeTab != null && UserControls[OpenPages.IndexOf(activeTab)] is TextEditorContainer textEditorContainer)
      {
        TextEditorContainer foundContainer = GetContainer(editorType);
        if (foundContainer == null)
        {
          return null;
        }
        else if (editorType == EditorType.TextEditor && string.Equals(activeTab.Text, editorType.ToString()))
        {
          return foundContainer.GetTextEditor();
        }
        else
        {
          return null;
        }
      }
      else
      {
        return null;
      }
    }

    public TextEditorUI GetActiveTextEditor()
    {
      var activeTab = OpenPages.FirstOrDefault(page => page.Background == (Brush)Application.Current.Resources["ActiveBorderSolidColorBrush"]);

      if (activeTab != null)
      {
        int index = OpenPages.IndexOf(activeTab);
        if (UserControls[index] is TextEditorContainer textEditorContainer)
        {
          var foundItem = textEditorContainer.DockManager.DockItems.FirstOrDefault(item => item.IsActiveDocument == true);
          if (foundItem != null)
          {
            if (foundItem.Content is TranslatorItem translatorItem)
            {
              return translatorItem.GetLeftEditor();
            }
            else if (foundItem.Content is TextEditorUI foundTextEditor)
            {
              return foundTextEditor;
            }
            else
            {
              return null;
            }
          }
          else
          {
            return null;
          }
        }
        else
        {
          return null;
        }
      }
      else
      {
        return null;
      }
    }

    /// <summary>
    /// Закрывает вкладку с активным текстовым редактором.
    /// </summary>
    /// <param name="isTranslation">Переменная, показывающая, выполняется закрытие вкладки при трансляции или нет.</param>
    /// <returns>Возвращает <c>true</c>, если вкладка была закрыта, <c>false</c> в противном случае.</returns>
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
          RemoveTextEditorContainer(textEditorContainer, EditorType.TextEditor);
        }

        return closed;
      }
      return false;
    }

    /// <summary>
    /// Отображает контейнер для вкладок заданного типа.
    /// </summary>
    /// <param name="textEditorContainer">Контейнер, содержащий вкладки заданного типа.</param>
    /// <param name="editorType">Тип вкладок контейнера.</param>
    private void ShowControl(TextEditorContainer textEditorContainer, EditorType editorType)
    {
      LogDebug($"Отображение контейнера для типа \"{editorType.ToString()}\"");
      var controlManager = new ControlManager(OpenPages, UserControls, FilePaths, multiEditorControl);
      var tabButton = new OpenFileButton();
      tabButton.Header.Text = editorType.ToString();
      controlManager.ShowControl(textEditorContainer, tabButton);
    }

    /// <summary>
    /// Получает контейнер заданного типа.
    /// </summary>
    /// <param name="editorType">Тип контейнера.</param>
    /// <returns>Найденный контейнер или <c>null</c>, если контнейнер не был найден.</returns>
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
        if (foundElement != null && foundElement is TextEditorContainer textEditorContainer)
        {
          return textEditorContainer;
        }
        else
        {
          return null;
        }
      }
    }

    /// <summary>
    /// Проверяет, существует ли уже открытый файл с таким названием, при необходимости присваивает файлу новое имя.
    /// </summary>
    /// <param name="path">Путь к файлу.</param>
    /// <param name="nameFile">Название файла.</param>
    /// <returns>Новое название файла.</returns>
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
    /// <param name="nameFile">Имя файла.</param>
    /// <param name="textEditorContainer">Контейнер редактора, в котором будут размещаться DockItem'ы.</param>
    /// <param name="textEditor">Экземпляр редактора для отображения содержимого файла.</param>
    internal async void ShowNewDockItem(string nameFile, TextEditorContainer textEditorContainer, UserControl textEditor, EditorType editorType = null)
    {
      LogDebug($"Создание DockItem для файла {nameFile}");
      var dockItem = new DockItem
      {
        Title = nameFile,
        TabText = nameFile,
        Content = textEditor
      };

      EventAggregator.OpenOpk -= OnOpenOpk;
      EventAggregator.OpenOpk += OnOpenOpk;

      if (dockItem.Content is TextEditorUI && editorType == EditorType.Archive || dockItem.Content is RunControl && editorType == EditorType.Run)
      {
        InitializeItemWithoutSave(dockItem, editorType);
      }
      else if (dockItem.Content is TextEditorUI || dockItem.Content is FileCompareControl)
      {
        editorType = EditorType.TextEditor;
        InitializeItemNeedSave(nameFile, textEditorContainer, textEditor, editorType, dockItem);
      }
      else if (dockItem.Content is TableAllArchivesControl || dockItem.Content is TableApkArchiveControl)
      {
        editorType = EditorType.Archive;
        editorType = InitializeItemWithoutSave(dockItem, editorType);
      }

      await Task.Delay(1).ConfigureAwait(true);

      ShowDockItem(textEditorContainer, dockItem);
      ShowControl(textEditorContainer, editorType);
    }

    private EditorType InitializeItemWithoutSave(DockItem dockItem, EditorType editorType)
    {
      dockItem.ItemClosed += (sender) =>
      {
        var controlManager = new ControlManager(OpenPages, UserControls, FilePaths, multiEditorControl);
        var foundPage = OpenPages.FirstOrDefault(page => page.Text == editorType.ToString());
        TextEditorContainer translatorContainer = GetContainer(editorType);

        if (translatorContainer != null && translatorContainer.DockManager.DockItems.Count(item => item.DockPosition != DockPosition.Hidden) == 0)
        {
          RemoveTextEditorContainer(translatorContainer, editorType);
        }
      };
      return editorType;
    }

    private void InitializeItemNeedSave(string nameFile, TextEditorContainer textEditorContainer, UserControl textEditor, EditorType editorType, DockItem dockItem)
    {
      LogDebug($"Тип редактора для файла {nameFile}: {editorType.ToString()}");

      dockItem.CloseItem += (sender) =>
      {
        LogDebug($"Закрытие файла {nameFile}.");

        if (textEditorContainer != null && editorType != null)
        {
          var controlManager = new ControlManager(OpenPages, UserControls, FilePaths, multiEditorControl);
          var foundPage = OpenPages.FirstOrDefault(page => page.Text == editorType.ToString());
          controlManager.RemoveControl(foundPage, textEditor).ConfigureAwait(true);
          FilePaths.Remove(dockItem.TabText);
          if (FilePaths.Count == 0)
          {
            LogDebug($"Закрытие контейнера типа \"{editorType.ToString()}\".");
            RemoveTextEditorContainer(textEditorContainer, editorType);
          }
          EventAggregator.RaiseTextEditorContainerClosing(true, nameFile);
        }
      };
    }

    /// <summary>
    /// Отображает новую вкладку с транслятором.
    /// </summary>
    /// <param name="nameFile">Название файла.</param>
    /// <param name="textEditorContainer">Контейнер для транслятора.</param>
    /// <param name="textEditor">Текстовый редактор с транслируемым документом.</param>
    /// <param name="translatorEditor">Текстовый редактор с странслированным документом.</param>
    /// <returns>Асинхронную задачу, представляющую результат создания экземпляра <see cref="TranslatorItem"/>.</returns>
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

        dockItem.ItemClosed += (sender) =>
        {
          var controlManager = new ControlManager(OpenPages, UserControls, FilePaths, multiEditorControl);
          var foundPage = OpenPages.FirstOrDefault(page => page.Text == EditorType.Translator.ToString());
          TextEditorContainer translatorContainer = GetContainer(EditorType.Translator);
          if (translatorContainer != null && translatorContainer.DockManager.DockItems.Count(item => item.DockPosition != DockPosition.Hidden) == 0)
          {
            RemoveTextEditorContainer(translatorContainer, EditorType.Translator);
          }
          EventAggregator.RaiseTextEditorContainerClosing(true, nameFile);
        };


        await Task.Delay(1).ConfigureAwait(true);

        ShowDockItem(textEditorContainer, dockItem);

        return translatorItem;
      }
      catch (Exception ex)
      {
        MessageBoxCustom.Show($"Системная ошибка: {ex}", "Ошибка!", MessageBoxButton.OK, MessageBoxImage.Error);
        LogError($"Системная ошибка: {ex}");
        return null;
      }
    }

    /// <summary>
    /// Удаляет контрол с котейнером для текстовых редакторов.
    /// </summary>
    /// <param name="textEditorContainer">Контейнер с текстовыми редакторами.</param>
    private void RemoveTextEditorContainer(TextEditorContainer textEditorContainer, EditorType editorType)
    {
      var controlManager = new ControlManager(OpenPages, UserControls, FilePaths, multiEditorControl);
      var foundPage = OpenPages.FirstOrDefault(page => page.Text == editorType.ToString());
      controlManager.RemoveControl(foundPage, textEditorContainer);
    }

    /// <summary>
    /// Отображает новую вкладку в контейнере.
    /// </summary>
    /// <param name="textEditorContainer">Контейнер с текстовыми редакторами, в котором необходимо открыть файл.</param>
    /// <param name="dockItem">Новая вкладка.</param>
    public void ShowDockItem(TextEditorContainer textEditorContainer, DockItem dockItem)
    {
      try
      {
        var dockControl = textEditorContainer?.DockManager;

        if (dockControl == null)
        {
          LogError("DockControl не найден (null). Невозможно отобразить вкладку.");
          return;
        }

        LogInformation($"Попытка показать DockItem. Title: {dockItem.Title}, IsLoaded: {dockControl.IsLoaded}, DockItems.Count: {dockControl.DockItems.Count}");

        if (!dockControl.IsLoaded)
        {
          LogWarning("DockControl ещё не загружен. Подписка на Loaded...");

          var capturedDockItem = dockItem;
          dockControl.Loaded += (s, e) =>
          {
            try
            {
              LogInformation("DockControl загрузился. Показываем вкладку.");
              capturedDockItem.Show(dockControl, DockPosition.Document);
              LogInformation("DockItem отображён после загрузки.");
            }
            catch (Exception ex)
            {
              LogException("Ошибка при отображении DockItem после загрузки:", ex);
            }
          };
        }
        else
        {
          dockItem.Show(dockControl, DockPosition.Document);
          LogInformation("DockItem отображён немедленно.");
        }
      }
      catch (Exception ex)
      {
        LogException("Ошибка при отображении DockItem:", ex);
      }
    }

    /// <summary>
    /// Определяет кодировку открываемого файла.
    /// </summary>
    /// <param name="filePath">Путь к файлу.</param>
    /// <returns>Кодировка файла.</returns>
    internal Encoding DetectEncodingFromFile(string filePath)
    {
      using var fileStream = File.OpenRead(filePath);
      return DetectEncodingFromText(fileStream);
    }

    internal static Encoding DetectEncodingFromText(FileStream fileStream)
    {
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

    /// <summary>
    /// Получает содержимое открываемого файла и кодировку, в которой записан файл.
    /// </summary>
    /// <param name="path">Путь к файлу.</param>
    /// <returns>Строку с содержимым файла и кодировку файла.</returns>
    private (string, Encoding) GetFileContent(string path)
    {
      Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

      Encoding encoding = DetectEncodingFromFile(path);

      var content = new List<string>();
      foreach (string line in File.ReadLines(path, encoding))
      {
        if (!string.IsNullOrEmpty(line))
        {
          content.Add(line);
        }
      }

      var fileContent = content.Count > 0 ? string.Join("\n", content) : string.Empty;

      return (fileContent, encoding);
    }

    /// <summary>
    /// Получает разницу в путях к файлам с одинаковым названием.
    /// </summary>
    /// <param name="existingPath">Уже открытый файл с тем же названием.</param>
    /// <param name="newPath">Открываемый файл.</param>
    /// <returns>Уникальное название открываемого файла.</returns>
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
    /// <param name="container">Экземпляр класса <see cref="UserControl"/>, представляющий собой контейнер.</param>
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
    /// Создает новый экземпляр <see cref="TextEditorUI"/> и устанавливает его текст.
    /// </summary>
    /// <param name="fileContent">Содержимое файла, которое будет установлено в редактор.</param>
    /// <returns>Новый экземпляр <see cref="TextEditorUI"/>.</returns>
    private TextEditorUI CreateTextEditor(TextEditorModel textEditorModel, string fileContent, FileType fileType = FileType.None)
    {
      var textEditor = new TextEditorUI(fileType, textEditorModel);
      textEditor.Text = fileContent;
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

      var textEditorModel = new TextEditorModel(controlName);
      textEditor.TextEditorModel = textEditorModel;
      ShowNewDockItem(controlName, textEditorContainer, textEditor);
      FilePaths.Add(controlName, string.Empty);
    }

    /// <summary>
    /// Сравнивает содержимое открытого файла с текстом в текущем редакторе.
    /// </summary>
    /// <param name="control">Экземпляр <see cref="DockItem"/>, представляющий открытую страницу.</param>
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
          MessageBoxCustom.Show("Файл был удален или поврежден", "Ошибка!", MessageBoxButton.OK, MessageBoxImage.Error);
        }
      }

      return false;
    }

    /// <summary>
    /// Определяет тип файла.
    /// </summary>
    /// <param name="fileName">Название файла.</param>
    /// <returns>Тип файла.</returns>
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

    /// <summary>
    /// Выполняет добавление <see cref="TranslatorItem"/> в качестве новой вкладки в DockControl.
    /// </summary>
    /// <param name="editor">Текстовый редактор с транслируемым файлом.</param>
    /// <param name="translateEditor">Текстовый редактор с странслированным файлом.</param>
    /// <param name="editorType">Тип контейнера.</param>
    /// <returns>Асинхронную задачу, представляющую результат выполнения.</returns>
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
        MessageBoxCustom.Show($"Ошибка при чтении файла: {ex.Message}", "Ошибка", image: MessageBoxImage.Error);
        LogException($"Ошибка при чтении файла", ex);
        return null;
      }
    }

    internal async Task AddRunItem(RunControl runControl, EditorType editorType)
    {
      try
      {
        TextEditorContainer textEditorContainer = GetContainer(editorType);
        if (textEditorContainer == null)
        {
          textEditorContainer = CreateContainer(editorType);
        }
        ShowNewDockItem($"{runControl.FileName}", textEditorContainer, runControl, editorType);

        ShowControl(textEditorContainer, EditorType.Translator);
      }
      catch (Exception ex)
      {
        MessageBoxCustom.Show($"Ошибка при чтении файла: {ex.Message}", "Ошибка", image: MessageBoxImage.Error);
        LogException($"Ошибка при чтении файла", ex);
        return;
      }
    }

    internal async Task DeleteTranslatorItem(TranslatorItem translatorItem, EditorType editorType)
    {
      try
      {
        TextEditorContainer textEditorContainer = GetContainer(editorType);
        if (textEditorContainer == null)
        {
          return;
        }

        textEditorContainer.RemoveTranslatorItem(translatorItem);
        if (textEditorContainer.DockManager.DockItems.Count == 0)
        {
          RemoveTextEditorContainer(textEditorContainer, EditorType.Translator);
        }
      }
      catch (Exception ex)
      {
        MessageBoxCustom.Show($"Ошибка при чтении файла: {ex.Message}", "Ошибка", image: MessageBoxImage.Error);
        LogException($"Ошибка при чтении файла", ex);
        return;
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

    /// <summary>
    /// Открывает папку в проводнике, в которой содержится файл.
    /// </summary>
    /// <param name="path">Путь к файлу.</param>
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

    internal async Task OpenArchiveAsync()
    {
      TextEditorContainer archiveContainer = GetContainer(EditorType.Archive);
      TableAllArchivesControl allArchives = null;
      if (archiveContainer == null)
      {
        archiveContainer = CreateContainer(EditorType.Archive);
      }
      if (archiveContainer.DockManager.DockItems.FirstOrDefault(item => item.TabText == "Все архивы") != null)
      {
        var foundDockItem = archiveContainer.DockManager.DockItems.FirstOrDefault(item => item.TabText == "Все архивы");
        if (foundDockItem.Content is TableAllArchivesControl archivesTable)
        {
          allArchives = archivesTable;
          ShowDockItem(archiveContainer, foundDockItem);
        }
      }
      else
      {
        allArchives = new TableAllArchivesControl();
        ShowNewDockItem("Все архивы", archiveContainer, allArchives);
      }

      ShowControl(archiveContainer, EditorType.Archive);
      allArchives.ArchiveSelected -= ArchiveControl_ArchiveSelected;
      allArchives.ArchiveSelected += ArchiveControl_ArchiveSelected;
    }

    private async void ArchiveControl_ArchiveSelected(object sender, MouseButtonEventArgs e)
    {
      var dataGrid = e.Source as DataGrid;
      if (dataGrid?.SelectedItem is ApkArchive selectedArchive)
      {
        if (selectedArchive != null)
        {
          TextEditorContainer archiveContainer = GetContainer(EditorType.Archive);
          var archiveName = selectedArchive.ArchiveName;
          ShowNewDockItem(archiveName, archiveContainer, new TableApkArchiveControl(archiveName));
          ShowControl(archiveContainer, EditorType.Archive);
        }
      }
    }

    private void OnOpenOpk(UserControl userControl, string elementName)
    {
      LogDebug($"Происходит открытие opk файла {elementName}.");
      TextEditorContainer archiveContainer = GetContainer(EditorType.Archive);
      var textEditor = userControl as TextEditorUI;
      textEditor.IsReadOnly = true;
      ShowNewDockItem(elementName, archiveContainer, textEditor, EditorType.Archive);
      ShowControl(archiveContainer, EditorType.Archive);
    }

    internal async Task CloseRunItem(RunControl runControl, EditorType editorType)
    {
      var controlManager = new ControlManager(this, multiEditorControl);
      TextEditorContainer runContainer = GetContainer(editorType);
      var foundTab = OpenPages.FirstOrDefault(tab => tab.Text == editorType.ToString());
      if (foundTab != null)
      {
        await controlManager.RemoveControl(foundTab, runControl);
      }
    }

    /// <summary>
    /// Конструктор для инициализации файлового менеджера.
    /// </summary>
    /// <param name="multiEditorControl">Экземпляр класса MultiEditorControl.</param>
    public FileManager(MultiEditorControl multiEditorControl)
    {
      LogDebug($"Создание экземпляра FileManager");
      this.FilePaths = new Dictionary<string, string>();
      this.UserControls = new ObservableCollection<UserControl>();
      this.OpenPages = new ObservableCollection<OpenFileButton>();
      this.multiEditorControl = multiEditorControl;
    }
  }
}
