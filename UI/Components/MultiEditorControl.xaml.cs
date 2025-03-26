using System.IO;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using UI.Components.Invoke;
using UI.Controls.TextEditor;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;
using PrintDialog = System.Windows.Controls.PrintDialog;
using UserControl = System.Windows.Controls.UserControl;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using AppConfig;
using System.Text.RegularExpressions;
using UI.Components.SearchControls;
using static Utilities.LoggerUtility;
using System.Text;
using ICSharpCode.AvalonEdit;
using System.Windows.Controls;
using ICSharpCode.AvalonEdit.Rendering;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using Path = System.IO.Path;
using ICSharpCode.AvalonEdit.Document;

namespace UI.Components
{
  /// <summary>
  /// Логика взаимодействия для MultiEditorControl.xaml
  /// </summary>
  public partial class MultiEditorControl : UserControl
  {
    List<OpenFileButton> openPages = new List<OpenFileButton>();
    List<UserControl> userControls = new List<UserControl>();

    Dictionary<string, string> filePaths = new Dictionary<string, string>();
    Dictionary<string, List<SearchResult>> foundInOpenedFiles = new Dictionary<string, List<SearchResult>>();

    private List<SearchResult> foundResults = new List<SearchResult>();
    private int currentIndex = -1;

    string _searchText;
    Dictionary<UserControl, string> _fullText = new Dictionary<UserControl, string>();
    bool? _wholeWord;
    bool? _caseWord;
    string _searchParameters;
    int _searchArea;
    int _textEditor;

    bool hasChanged;

    private int _clickCount = 0;
    private int _editorCount = 0;
    private DispatcherTimer _clickTimer;

    public event Action<string, Dictionary<string, List<SearchResult>>> SearchResultsReady;
    private Dictionary<string, (int lineNumber, int lineLength)> _pendingHighlights = new Dictionary<string, (int lineNumber, int lineLength)>();

    private static ProgressWindow _progressWindow;

    public MultiEditorControl()
    {
      InitializeComponent();
      _clickTimer = new DispatcherTimer
      {
        Interval = TimeSpan.FromMilliseconds(300)
      };

      _clickTimer.Tick += (s, e) =>
      {
        _clickCount = 0;
        _clickTimer.Stop();
      };

      this.KeyDown += MultiWindowControl_KeyDown;
      EventAggregator.FoundTextSelectRow += OnFoundTextSelectRow;
    }

    private void InitializeTextMarkerService(TextEditorUI textEditorUI)
    {
      if (textEditorUI.MarkerService != null)
        return;

      var textEditor = textEditorUI.TextEditor;
      var markerService = new TextMarkerService(textEditor.Document, textEditor);
      textEditor.TextArea.TextView.BackgroundRenderers.Add(markerService);
      textEditor.TextArea.TextView.LineTransformers.Add(markerService);

      textEditorUI.MarkerService = markerService;
    }


    private void TopPanel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
      _clickCount++;

      if (_clickCount == 1)
      {
        _clickTimer.Start();
      }
      else if (_clickCount == 2)
      {
        _clickTimer.Stop();
        _clickCount = 0;
        CreateNewFile();
      }
    }

    public TextEditorUI GetActiveTextEditor()
    {
      var activePage = openPages.FirstOrDefault(page =>
                        page.Background == (Brush)Application.Current.Resources["ActiveBorderSolidColorBrush"]);
      if (activePage != null)
      {
        int index = openPages.IndexOf(activePage);
        if (userControls[index] is TextEditorUI activeEditor)
        {
          return activeEditor;
        }
      }
      return null;
    }

    /// <summary>
    /// Добавляет элемент управления и кнопку в соответствующие панели.
    /// </summary>
    /// <param name="header">Заголовок для кнопки.</param>
    /// <param name="control">Элемент управления для отображения.</param>
    public void AddControl(string header, UserControl control, string description = null)
    {
      OpenFileButton tabButton = new OpenFileButton();
      tabButton.Header.Text = header;
      if (description != null)
      {
        tabButton.Description = description;

        foreach (OpenFileButton page in openPages)
        {
          if (page.Description == description)
          {
            var index = openPages.IndexOf(page);
            var userControl = userControls[index];
            ShowControl(userControl, page);
            return;
          }
        }
      }
      else
      {
        foreach (OpenFileButton page in openPages)
        {
          if (page.Header.Text == header)
          {
            var index = openPages.IndexOf(page);
            var userControl = userControls[index];
            ShowControl(userControl, page);

            return;
          }
        }
      }

      tabButton.PreviewMouseDown += (s, e) => ShowControl(control, tabButton);
      tabButton.GetCloseButton().PreviewMouseDown += (s, e) => RemoveControl(tabButton, control);
      tabButton.MouseDown += (s, e) =>
      {
        if (e.ChangedButton == MouseButton.Middle)
        {
          RemoveControl(tabButton, control);
        }
      };

      openPages.Add(tabButton);
      userControls.Add(control);

      try
      {
        ContentPanel.Children.Add(control);
        TopPanel.Children.Add(tabButton);
      }
      finally
      {
        ShowControl(control, tabButton);
      }
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

        AddControl(nameFile, textEditor);
        if (!filePaths.ContainsKey(nameFile))
        {
          filePaths.Add(nameFile, path);
        }
        InitializeTextMarkerService(textEditor);
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
      while (filePaths.ContainsKey(controlName))
      {
        counter++;
        if (controlName != "Новый")
        {
          controlName = controlName.Remove(controlName.Length - (counter - 1).ToString().Length, (counter - 1).ToString().Length);
        }
        controlName += $"{counter}";
      }
      var textEditor = new TextEditorUI();
      AddControl(controlName, textEditor /*{ Text  = "Новый файл"}*/);
      filePaths.Add(controlName, string.Empty);
      InitializeTextMarkerService(textEditor);
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

    /// <summary>
    /// Отображает указанный элемент управления, скрывая остальные.
    /// </summary>
    /// <param name="control">Элемент управления для отображения.</param>
    private void ActivePage(OpenFileButton control)
    {
      foreach (OpenFileButton child in TopPanel.Children)
      {
        if (control == child)
        {
          child.Background = (Brush)Application.Current.Resources["ActiveBorderSolidColorBrush"];
        }
        else
        {
          child.Background = (Brush)Application.Current.Resources["SecondarySolidColorBrush"];
        }
      }
    }

    /// <summary>
    /// Отображает указанный элемент управления, скрывая остальные.
    /// </summary>
    /// <param name="control">Элемент управления для отображения.</param>
    private void ShowControl(UserControl control, OpenFileButton openPage)
    {
      foreach (UIElement child in ContentPanel.Children)
      {
        child.Visibility = child == control ? Visibility.Visible : Visibility.Collapsed;
      }

      ActivePage(openPage);

      if (control is TextEditorUI textEditor)
      {
        string fileName = openPage.Text;
        if (_pendingHighlights.TryGetValue(fileName, out var highlightInfo))
        {
          textEditor.ScrollToLine(highlightInfo.lineNumber);

          int startOffset = textEditor.Document.GetOffset(
              highlightInfo.lineNumber, 1);
          ApplyHighlightingWhenRendered(textEditor, startOffset, highlightInfo.lineLength);

          _pendingHighlights.Remove(fileName);
        }
      }

      bool isTextEditor = control is TextEditorUI;
      EventAggregator.RaiseTextEditorActive(isTextEditor);
      EventAggregator.RaiseActiveEditorChanged(isTextEditor);
    }

    /// <summary>
    /// Удаляет указанный элемент управления и соответствующую вкладку.
    /// </summary>
    /// <param name="tabButton">Вкладка для удаления.</param>
    /// <param name="control">Элемент управления для удаления.</param>
    private void RemoveControl(OpenFileButton tabButton, UserControl control)
    {
      if (openPages.Contains(tabButton) && userControls.Contains(control))
      {
        var result = MessageBoxResult.No;
        var saveFileResult = false;
        int index = ContentPanel.Children.IndexOf(control);
        if (control is TextEditorUI)
        {
          SaveFileDialog(ref result, ref saveFileResult, index);
        }
        if (saveFileResult == true || !(control is TextEditorUI) || result == MessageBoxResult.No)
        {
          if (index > 0)
          {
            index--;
          }
          EventAggregator.RaiseTextEditorClosing(control is TextEditorUI, tabButton.Text);


          openPages.Remove(tabButton);
          userControls.Remove(control);

          TopPanel.Children.Remove(tabButton);
          ContentPanel.Children.Remove(control);

          if (ContentPanel.Children.Count > 0)
          {
            ShowControl(userControls[index], openPages[index]);
          }
        }
      }
    }

    private void SaveFileDialog(ref MessageBoxResult result, ref bool saveFileResult, int index)
    {
      var needToSave = CompareFiles(openPages[index]);
      if (needToSave)
      {
        result = MessageBox.Show(
            $"Сохранить файл {openPages[index].Text} перед закрытием?",
            "Подтверждение",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        if (result == MessageBoxResult.Yes)
        {
          saveFileResult = SaveFile(openPages[index]);
        }
      }
    }

    private void MultiWindowControl_KeyDown(object sender, KeyEventArgs e)
    {
      Console.WriteLine($"e.Key = {e.Key}; e.SystemKey = {e.SystemKey}; Keyboard.Modifiers = {Keyboard.Modifiers}");

      if (e.Key == Key.System && e.SystemKey == Key.X && Keyboard.Modifiers == ModifierKeys.Alt)
      {
        var activeTab = openPages.FirstOrDefault(page => page.Background == (Brush)Application.Current.Resources["ActiveBorderSolidColorBrush"]);
        if (activeTab != null)
        {
          int index = openPages.IndexOf(activeTab);
          RemoveControl(activeTab, userControls[index]);
        }
      }
    }

    private bool CompareFiles(OpenFileButton openPage)
    {
      var activeTab = openPages.FirstOrDefault(page => page.Background == (Brush)Application.Current.Resources["ActiveBorderSolidColorBrush"]);
      if (activeTab != null)
      {
        var fileName = activeTab.Text;
        if (filePaths[fileName] == string.Empty)
        {
          return true;
        }
        else
        {
          var filePath = filePaths[fileName];
          var content = File.ReadAllText(filePath);
          int index = openPages.IndexOf(activeTab);

          if (userControls[index] is TextEditorUI)
          {
            var textEditor = userControls[index] as TextEditorUI;
            return content != textEditor.Text;
          }

          return false;
        }
      }
      return false;
    }

    #region Сохранение файлов

    public bool SaveFile()
    {
      var activeTab = openPages.FirstOrDefault(page => page.Background == (Brush)Application.Current.Resources["ActiveBorderSolidColorBrush"]);
      return SaveFile(activeTab);
    }

    private bool SaveFile(OpenFileButton activeTab)
    {
      if (activeTab != null)
      {
        var fileName = activeTab.Text;
        if (filePaths[fileName] == string.Empty)
        {
          return SaveFileAs();
        }
        else
        {
          var filePath = filePaths[fileName];
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
        var activeTab = openPages.FirstOrDefault(page => page.Background == (Brush)Application.Current.Resources["ActiveBorderSolidColorBrush"]);
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
            if (!filePaths.ContainsKey(fileName))
            {
              filePaths.Add(fileName, filePath);
            }
            else
            {
              filePaths[fileName] = filePath;
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

      int index = openPages.IndexOf(activeTab);
      if (userControls[index] is TextEditorUI)
      {
        var textEditor = userControls[index] as TextEditorUI;
        fileData = textEditor.Text;
        File.WriteAllText(filePath, fileData);
        LogInformation($"Файл {filePath} сохранен");
        MessageBox.Show($"Файл {filePath} сохранен");
        return true;
      }
      return false;
    }

    #endregion

    private void RenamePage(OpenFileButton activeTab, string filePath)
    {
      var acivePage = openPages.FirstOrDefault(p => p == activeTab);
      if (acivePage != null)
      {
        activeTab.Header.Text = System.IO.Path.GetFileName(filePath);
      }
    }

    #region Печать файлов

    public void PrintFile()
    {
      var activeTab = openPages.FirstOrDefault(page => page.Background == (Brush)Application.Current.Resources["ActiveBorderSolidColorBrush"]);
      PrintDialog printDialog = new PrintDialog();
      FlowDocument flowDocument = new FlowDocument();


      if (printDialog.ShowDialog() == true)
      {
        int index = openPages.IndexOf(activeTab);

        if (userControls[index] is TextEditorUI)
        {
          var textEditor = userControls[index] as TextEditorUI;
          flowDocument.Blocks.Add(new Paragraph(new Run(textEditor.Text)));
          IDocumentPaginatorSource idocument = flowDocument;
          printDialog.PrintDocument(idocument.DocumentPaginator, "Печать документа");
          LogInformation($"Файл {activeTab.Text} отправлен на печать");
        }
      }
    }

    #endregion

    #region Поиск по тексту

    // TODO: поиск по тексту делать тут
    /// <summary>
    /// Выполняет поиск по тектсу.
    /// </summary>
    /// <param name="searchText">Текст, который мы ищем.</param>
    /// <param name="wholeWord">Если true - ищем только слово целиком, false - ищем все вхождения заданного текста.</param>
    /// <param name="caseWord">Если true - учитываем регистр, false - не учитываем.</param>
    /// <param name="searchArea">Параметры поиска: найти  далее, найти предыдущее, найти все.</param>
    /// <param name="searchParameters">Область поиска: поиск в текущем документе, во всех открытых документах, в файле.</param>
    public async Task SearchData(string searchText, bool? wholeWord, bool? caseWord, int searchArea, string searchParameters)
    {
      if (searchArea == 2)
      {
        searchArea = 0;
      }
      var fullText = GetText(searchArea);
      if (!string.Equals(searchText, _searchText))
      {
        foreach (var key in fullText.Keys)
        {
          if (key is TextEditorUI textEditor)
          {
            ClearHighlights(textEditor);
          }
        }
      }
      InitializeSearch(fullText, searchText, wholeWord, caseWord, searchArea, searchParameters); // тут какой то косяк опять

      if (hasChanged)
      {
        if (searchParameters == "FindAll")
        {
          await FindAllAsync(searchText, wholeWord, caseWord, searchArea);
        }
        else
        {
          var activeTab = openPages.FirstOrDefault(page => page.Background == (Brush)Application.Current.Resources["ActiveBorderSolidColorBrush"]);
          var pageIndex = openPages.IndexOf(activeTab);
          var pageName = openPages[pageIndex].Text;
          var allOccurrences = FindAllOccurrences(fullText[userControls[pageIndex]], searchText, wholeWord, caseWord, searchArea);
          if (allOccurrences != null)
          {
            InitializeCurrentIndex();
            GoToOccurrence(currentIndex);
          }
        }
      }
      else
      {
        var activeTab = openPages.FirstOrDefault(page =>
                      page.Background == (Brush)Application.Current.Resources["ActiveBorderSolidColorBrush"]);
        if (activeTab != null)
        {
          int pageIndex = openPages.IndexOf(activeTab);
          if (userControls[pageIndex] is TextEditorUI textEditor)
          {
            if (searchParameters == "FindNext")
            {
              NextOccurrence(searchArea, textEditor);
            }
            if (searchParameters == "FindPrevious")
            {
              PreviousOccurrence(searchArea, textEditor);
            }
            if (searchParameters == "FindAll")
            {
              await FindAllAsync(searchText, wholeWord, caseWord, searchArea);
            }
          }
        }
      }
    }


    public async Task FindAllAsync(string searchText, bool? wholeWord, bool? caseWord, int searchArea)
    {
      if (string.IsNullOrEmpty(searchText))
      {
        MessageBox.Show("Введите текст для поиска.");
        LogWarning("Поле для поиска не заполнено");
        return;
      }

      List<OpenFileButton> searchPages = SetSearchAreaPages(searchArea);

      if (foundInOpenedFiles.Count > 0)
      {
        foundInOpenedFiles.Clear();
      }

      EventAggregator.RaiseRequestShowProgress();
      List<Task> tasks = ExecuteSearchTasks(searchPages, searchText, wholeWord, caseWord);
      await Task.WhenAll(tasks);
      EventAggregator.RaiseRequestCloseProgress();

      if (foundInOpenedFiles.Count > 0)
      {
        var lastFoundResultsDictionary = foundInOpenedFiles.Values.LastOrDefault();
        if (lastFoundResultsDictionary?.Count > 0)
        {
          DisplaySearchResults(searchText, foundInOpenedFiles);
        }
      }
      else
      {
        MessageBox.Show("Текст не найден в открытых документах.");
        LogInformation("Текст не найден в открытых документах.");
        return;
      }
    }

    /// <summary>
    /// Выполняет поиск для каждой вкладки в отдельном Task.
    /// </summary>
    private List<Task> ExecuteSearchTasks(List<OpenFileButton> searchPages, string searchText, bool? wholeWord, bool? caseWord)
    {
      object lockObj = new object();

      return searchPages.Select(page => Task.Run(() =>
      {
        // Получаем имя вкладки через Dispatcher (на UI-потоке)
        string pageName = page.Dispatcher.Invoke(() => page.Text);

        int pageIndex = openPages.IndexOf(page);
        if (userControls[pageIndex] is TextEditorUI textEditor)
        {
          // Получаем текст редактора через Dispatcher
          string pageText = textEditor.Dispatcher.Invoke(() => textEditor.Text);
          var foundResultsList = FindOccurrencesByLine(pageText, searchText, wholeWord, caseWord);
          if (foundResultsList!=null && foundResultsList.Count > 0)
          {
            lock (lockObj)
            {
              foundInOpenedFiles[pageName] = foundResultsList;
            }
          }
        }
      })).ToList();
    }

    private List<OpenFileButton> SetSearchAreaPages(int searchArea)
    {
      var searchPages = new List<OpenFileButton>();
      if (searchArea == 0 || searchArea == 2)
      {
        var activeTab = openPages.FirstOrDefault(page => page.Background == (Brush)Application.Current.Resources["ActiveBorderSolidColorBrush"]);
        var pageIndex = openPages.IndexOf(activeTab);
        searchPages.Add(openPages[pageIndex]);
      }
      else
      {
        searchPages = openPages;
      }

      return searchPages;
    }

    public void DisplaySearchResults(string searchText, Dictionary<string, List<SearchResult>> results)
    {
      if (results == null || results.Count == 0)
      {
        MessageBox.Show("Результаты поиска пусты!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
        return;
      }
      SearchResultsReady?.Invoke(searchText, results);
    }

    private Dictionary<UserControl, string> GetText(int searchArea)
    {
      OpenFileButton? activeTab;
      int pageIndex = -1;
      Dictionary<UserControl, string> fullText = new Dictionary<UserControl, string>();

      if (searchArea == 0 || searchArea == 2)
      {
        activeTab = openPages.FirstOrDefault(page => page.Background == (Brush)Application.Current.Resources["ActiveBorderSolidColorBrush"]);
        pageIndex = openPages.IndexOf(activeTab);
        if (pageIndex != -1 && userControls[pageIndex] is TextEditorUI textEditor)
        {
          fullText.Add(textEditor, textEditor.Text);
        }
      }
      else
      {
        var foundResultsDictionary = new Dictionary<int, string>();
        foreach (var page in openPages)
        {
          pageIndex = openPages.IndexOf(page);
          if (userControls[pageIndex] is TextEditorUI textEditor)
          {
            if (!fullText.ContainsKey(textEditor))
            {
              var pageText = textEditor.Text;
              fullText.Add(textEditor, pageText);
            }
          }
        }
      }

      return fullText;
    }

    /// <summary>
    /// Инициализирует параметры поиска по тектсу.
    /// </summary>
    /// <param name="searchText">Текст, который мы ищем.</param>
    /// <param name="wholeWord">Если true - ищем только слово целиком, false - ищем все вхождения заданного текста.</param>
    /// <param name="caseWord">Если true - учитываем регистр, false - не учитываем.</param>
    /// <param name="searchArea">Параметры поиска: найти  далее, найти предыдущее, найти все.</param>
    /// <param name="searchParameters">Область поиска: поиск в текущем документе, во всех открытых документах, в файле.</param>
    private void InitializeSearch(Dictionary<UserControl, string> fullText, string searchText, bool? wholeWord, bool? caseWord, int searchArea, string searchParameters)
    {
      bool significantSearchParametersChanged = !string.Equals(_searchParameters, searchParameters)
        && !((string.Equals(_searchParameters, "FindPrevious") && string.Equals(searchParameters, "FindNext"))
        || (string.Equals(_searchParameters, "FindNext") && string.Equals(searchParameters, "FindPrevious")));

      if ((!Enumerable.SequenceEqual(_fullText, fullText))
              || (!string.Equals(_searchText, searchText))
              || significantSearchParametersChanged
              || _wholeWord != wholeWord
              || _caseWord != caseWord
              || _searchArea != searchArea)
      {
        _fullText = fullText;
        _searchText = searchText;
        _wholeWord = wholeWord;
        _caseWord = caseWord;
        _searchArea = searchArea;
        _searchParameters = searchParameters;
        hasChanged = true;
      }
      else
      {
        hasChanged = false;
      }
    }


    public List<SearchResult> FindAllOccurrences(string fullText, string searchText, bool? wholeWord, bool? caseWord, int searchArea)
    {
      ClearAllHighlights();

      if (!ValidateSearchText(searchText))
      {
        return null;
      }

      string pattern = BuildRegexPattern(searchText, wholeWord);
      RegexOptions options = caseWord == true ? RegexOptions.None : RegexOptions.IgnoreCase;

      MatchCollection matches = Regex.Matches(fullText, pattern, options);

      ProcessMatches(matches);

      if (foundResults.Count > 0)
      {
        return foundResults;
      }
      else
      {
        MessageBox.Show("Текст не найден.");
        LogInformation($"Текст {searchText} не найден.");
        return null;
      }
    }

    /// <summary>
    /// Очищает подсветку для всех текстовых редакторов.
    /// </summary>
    private void ClearAllHighlights()
    {
      foreach (var userControl in userControls)
      {
        if (userControl is TextEditorUI textEditor)
        {
          ClearHighlights(textEditor);
        }
      }
    }

    /// <summary>
    /// Проверяет, что текст для поиска не пустой.
    /// </summary>
    private bool ValidateSearchText(string searchText)
    {
      if (string.IsNullOrEmpty(searchText))
      {
        MessageBox.Show("Введите текст для поиска.");
        LogWarning("Поле для поиска не заполнено");
        return false;
      }
      return true;
    }

    /// <summary>
    /// Формирует регулярное выражение для поиска, учитывая параметр wholeWord.
    /// </summary>
    private string BuildRegexPattern(string searchText, bool? wholeWord)
    {
      // Экранируем спецсимволы
      searchText = Regex.Escape(searchText);
      return wholeWord == true ? $@"\b{searchText}\b" : searchText;
    }

    /// <summary>
    /// Обрабатывает найденные совпадения и заполняет коллекцию foundResults.
    /// </summary>
    private void ProcessMatches(MatchCollection matches)
    {
      // Очищаем старые результаты
      foundResults.Clear();

      foreach (Match match in matches)
      {
        foundResults.Add(new SearchResult(match.Index, match.Length));
      }
    }

    /// <summary>
    /// Инициализирует значение currentIndex, если оно не задано.
    /// </summary>
    private void InitializeCurrentIndex()
    {
      if (currentIndex == -1)
      {
        if (_searchParameters == "FindNext")
        {
          currentIndex = 0;
        }
        else if (_searchParameters == "FindPrevious")
        {
          // Пример: берем последний индекс
          currentIndex = (foundResults.Count - 1);
        }
      }
    }

    public List<SearchResult> FindOccurrencesByLine(string fullText, string searchText, bool? wholeWord, bool? caseWord, int lineOffset = 0)
    {
      List<SearchResult> results = new List<SearchResult>();
      if (string.IsNullOrEmpty(searchText))
        return results;

      RegexOptions options = caseWord == true ? RegexOptions.None : RegexOptions.IgnoreCase;
      string escapedSearchText = Regex.Escape(searchText);
      string pattern = wholeWord == true ? $@"\b{escapedSearchText}\b" : escapedSearchText;

      // Разбиваем текст на строки
      string[] lines = fullText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

      for (int i = 0; i < lines.Length; i++)
      {
        string line = lines[i];
        MatchCollection matches = Regex.Matches(line, pattern, options);
        foreach (Match match in matches)
        {
          int matchIndex = match.Index;
          // Находим начало слова в строке, в которой найдено совпадение
          int wordStart = 0;
          var preMatch = Regex.Match(line.Substring(0, matchIndex), @"\b\w*$");
          if (preMatch.Success)
            wordStart = preMatch.Index;
          else
            wordStart = matchIndex;

          // Получаем подстроку, начиная с найденного слова до конца строки
          string substringFromWord = line.Substring(wordStart);

          // Абсолютное смещение = lineOffset (начало строки в документе) + match.Index
          int absoluteIndex = lineOffset + match.Index;

          // Создаем SearchResult с дополнительной информацией
          results.Add(new SearchResult(absoluteIndex, match.Length, i + 1, wordStart, substringFromWord));
        }
        // Обновляем базовое смещение для следующей строки
        lineOffset += line.Length + Environment.NewLine.Length;
      }

      return results.Count > 0 ? results : null;
    }


    private void HighlightText(int startOffset, int length, TextEditorUI textEditor)
    {
      var marker = textEditor.MarkerService.Create(startOffset, length);
      marker.BackgroundColor = (Color)Application.Current.Resources["ActiveColor"];
      marker.ForegroundColor = Colors.Black;
      textEditor.TextArea.TextView.Redraw();
      //textEditor.TextArea.TextView.InvalidateLayer(KnownLayer.Selection);
    }

    /// <summary>
    /// Ждёт, пока визуальный слой TextView обновится, и затем выполняет подсветку.
    /// Это помогает, если документ ещё не отрендерен (например, вкладка была неактивна).
    /// </summary>
    /// <param name="textEditor">Текущий редактор.</param>
    /// <param name="startOffset">Начальное смещение для подсветки.</param>
    /// <param name="length">Длина подсветки.</param>
    private void ApplyHighlightingWhenRendered(TextEditorUI textEditor, int startOffset, int length)
    {
      EventHandler handler = null;
      handler = (s, e) =>
      {
        // Отписываемся, чтобы обработчик сработал только один раз
        textEditor.TextArea.TextView.LayoutUpdated -= handler;
        // Вызываем подсветку
        HighlightText(startOffset, length, textEditor);
      };
      textEditor.TextArea.TextView.LayoutUpdated += handler;
    }


    /// <summary>
    /// Переход к следующему вхождению.
    /// </summary>
    private void NextOccurrence(int searchArea, TextEditorUI textEditor)
    {
      if (foundResults.Count > 0)
      {
        currentIndex++;
        textEditor.MarkerService.RemoveAll();

        if (currentIndex >= foundResults.Count)
        {
          int index = -1;
          if (searchArea == 1)
          {
            index = SwitchToNextDocument();
            if (index > -1)
            {
              var allOccurrences = FindAllOccurrences(_fullText.Values.ElementAt(index), _searchText, _wholeWord, _caseWord, searchArea);
              if (allOccurrences != null)
              {
                InitializeCurrentIndex();
                GoToOccurrence(currentIndex);
              }
            }
            if (index == -1)
            {
              MessageBox.Show("Достигнуто последнее совпадение. Дополнительных вхождений в тексте нет.", "Поиск закончен");
              return;
            }
          }
          currentIndex = 0;
        }

        GoToOccurrence(currentIndex);
      }
    }

    private int SwitchToNextDocument()
    {
      int currentIndex = openPages.FindIndex(p => p.Background == (Brush)Application.Current.Resources["ActiveBorderSolidColorBrush"]);
      var nextIndex = currentIndex + 1;

      if (currentIndex >= 0 && currentIndex < openPages.Count - 1)
      {
        var nextPage = openPages[nextIndex];
        while (!(userControls[nextIndex] is TextEditorUI))
        {
          nextIndex++;
          nextPage = openPages[nextIndex];
        }
        _textEditor++;
        Application.Current.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Render);
        ShowControl(userControls[nextIndex], nextPage);
        return _textEditor;
      }
      if (nextIndex > openPages.Count - 1)
      {
        nextIndex = 0;
        _textEditor = -1;
        var nextPage = openPages[nextIndex];
        while (!(userControls[nextIndex] is TextEditorUI))
        {
          nextIndex++;
          nextPage = openPages[nextIndex];
        }
        if (nextIndex >= openPages.Count)
        {
          return -1;
        }
        else
        {
          _textEditor++;
          ShowControl(userControls[nextIndex], nextPage);
          return _textEditor;
        }
      }
      return -1;
    }



    /// <summary>
    /// Переход к предыдущему вхождению.
    /// </summary>
    private void PreviousOccurrence(int searchArea, TextEditorUI textEditor)
    {
      if (foundResults.Count == 0) return;

      textEditor.MarkerService.RemoveAll();
      currentIndex = (currentIndex - 1 + foundResults.Count) % foundResults.Count;
      if (currentIndex >= foundResults.Count)
      {
        if (searchArea == 1)
        {
          SwitchToNextDocument();
        }
        currentIndex = 0;
      }
      GoToOccurrence(currentIndex);
    }

    /// <summary>
    /// Переход к определенному вхождению.
    /// </summary>
    private void GoToOccurrence(int index)
    {
      if (index >= 0 && index < foundResults.Count)
      {
        var activeTab = openPages.FirstOrDefault(page => page.Background == (Brush)Application.Current.Resources["ActiveBorderSolidColorBrush"]);
        int pageIndex = openPages.IndexOf(activeTab);

        if (userControls[pageIndex] is TextEditorUI textEditor)
        {
          var result = foundResults[index];
          ApplyHighlightingWhenRendered(textEditor, result.StartOffset, result.Length);
          textEditor.TextArea.TextView.InvalidateLayer(KnownLayer.Selection);
          int lineNumber = textEditor.Document.GetLineByOffset(result.StartOffset).LineNumber;
          textEditor.ScrollToLine(lineNumber);
          textEditor.Select(result.StartOffset, result.Length);
          textEditor.Focus();
        }
      }
    }


    /// <summary>
    /// Очистка подстветки.
    /// </summary>
    private void ClearHighlights(TextEditorUI textEditor)
    {
      textEditor.MarkerService.RemoveAll();
      foundResults.Clear();
      textEditor.TextArea.SelectionBorder = null;
      currentIndex = -1;
    }

    /// <summary>
    /// Очистка подстветки.
    /// </summary>
    public void OnSearchWindowClosing()
    {
      foreach (var control in userControls)
      {
        if (control is TextEditorUI textEditor)
        {
          ClearHighlights(textEditor);
        }
      }
      _pendingHighlights.Clear();
      _fullText = new Dictionary<UserControl, string>();
      //_searchText = string.Empty;
      _wholeWord = false;
      _caseWord = false;
      _searchArea = 0;
      _searchParameters = string.Empty;
      hasChanged = true;
    }

    private void OnFoundTextSelectRow(string fileName, int lineNumber, int startOffset, string lineText)
    {
      GetLineOccurrences(fileName, lineNumber, startOffset, lineText);
    }

    private void GetLineOccurrences(string fileName, int lineNumber, int startOffset, string lineText)
    {
      var foundPage = openPages.FirstOrDefault(page => page.Text == fileName);
      if (foundPage != null)
      {
        int pageIndex = openPages.IndexOf(foundPage);

        if (userControls[pageIndex] is TextEditorUI textEditor)
        {
          textEditor.MarkerService.RemoveAll();
          ShowControl(textEditor, foundPage);
          ApplyHighlightingWhenRendered(textEditor, startOffset, _searchText.Length);
          textEditor.TextArea.TextView.InvalidateLayer(KnownLayer.Selection);
          textEditor.ScrollToLine(lineNumber);
          textEditor.Select(startOffset, _searchText.Length);
          textEditor.Focus();
        }
      }
    }

    #endregion
  }
}
