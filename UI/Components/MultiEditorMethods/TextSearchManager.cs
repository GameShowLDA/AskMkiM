using AppConfig;
using ICSharpCode.AvalonEdit.Rendering;
using ICSharpCode.AvalonEdit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using UI.Components.Invoke;
using UI.Controls.TextEditor;
using System.Windows.Threading;
using System.Windows.Media;
using UI.Components.SearchControls;
using static Utilities.LoggerUtility;
using System.Windows.Controls;


namespace UI.Components.MultiEditorMethods
{
  public class TextSearchManager
  {
    //List<OpenFileButton> openPages = new List<OpenFileButton>();
    //List<UserControl> userControls = new List<UserControl>();

    //Dictionary<string, string> filePaths = new Dictionary<string, string>();
    private FileManager fileManager { get; set; }
    private MultiEditorControl multiEditorControl { get; set; }
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

    public event Action<string, bool?, Dictionary<string, List<SearchResult>>> SearchResultsReady;
    private Dictionary<string, (int lineNumber, int lineLength)> _pendingHighlights = new Dictionary<string, (int lineNumber, int lineLength)>();

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
          var activeTab = fileManager.OpenPages.FirstOrDefault(page => page.Background == (Brush)Application.Current.Resources["ActiveBorderSolidColorBrush"]);
          var pageIndex = fileManager.OpenPages.IndexOf(activeTab);
          var pageName = fileManager.OpenPages[pageIndex].Text;
          var allOccurrences = FindAllOccurrences(fullText[fileManager.UserControls[pageIndex]], searchText, wholeWord, caseWord, searchArea);
          if (allOccurrences != null)
          {
            InitializeCurrentIndex();
            GoToOccurrence(currentIndex);
          }
        }
      }
      else
      {
        var activeTab = fileManager.OpenPages.FirstOrDefault(page =>
                      page.Background == (Brush)Application.Current.Resources["ActiveBorderSolidColorBrush"]);
        if (activeTab != null)
        {
          int pageIndex = fileManager.OpenPages.IndexOf(activeTab);
          if (fileManager.UserControls[pageIndex] is TextEditorUI textEditor)
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

      if (foundInOpenedFiles.Count > 0)
      {
        var lastFoundResultsDictionary = foundInOpenedFiles.Values.LastOrDefault();
        if (lastFoundResultsDictionary?.Count > 0)
        {
          DisplaySearchResults(searchText, caseWord, foundInOpenedFiles);
        }
        EventAggregator.RaiseRequestCloseProgress();
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
        string pageName = page.Dispatcher.Invoke(() => page.Text);

        int pageIndex = fileManager.OpenPages.IndexOf(page);
        if (fileManager.UserControls[pageIndex] is TextEditorUI textEditor)
        {
          string pageText = textEditor.Dispatcher.Invoke(() => textEditor.Text);
          var foundResultsList = FindOccurrencesByLine(pageText, searchText, wholeWord, caseWord);
          if (foundResultsList != null && foundResultsList.Count > 0)
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
        var activeTab = fileManager.OpenPages.FirstOrDefault(page => page.Background == (Brush)Application.Current.Resources["ActiveBorderSolidColorBrush"]);
        var pageIndex = fileManager.OpenPages.IndexOf(activeTab);
        searchPages.Add(fileManager.OpenPages[pageIndex]);
      }
      else
      {
        searchPages = fileManager.OpenPages;
      }

      return searchPages;
    }

    public void DisplaySearchResults(string searchText, bool? isCaseSensetive, Dictionary<string, List<SearchResult>> results)
    {
      if (results == null || results.Count == 0)
      {
        MessageBox.Show("Результаты поиска пусты!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
        return;
      }
      SearchResultsReady?.Invoke(searchText, isCaseSensetive, results);
    }

    private Dictionary<UserControl, string> GetText(int searchArea)
    {
      OpenFileButton? activeTab;
      int pageIndex = -1;
      Dictionary<UserControl, string> fullText = new Dictionary<UserControl, string>();

      if (searchArea == 0 || searchArea == 2)
      {
        activeTab = fileManager.OpenPages.FirstOrDefault(page => page.Background == (Brush)Application.Current.Resources["ActiveBorderSolidColorBrush"]);
        pageIndex = fileManager.OpenPages.IndexOf(activeTab);
        if (pageIndex != -1 && fileManager.UserControls[pageIndex] is TextEditorUI textEditor)
        {
          fullText.Add(textEditor, textEditor.Text);
        }
      }
      else
      {
        var foundResultsDictionary = new Dictionary<int, string>();
        foreach (var page in fileManager.OpenPages)
        {
          pageIndex = fileManager.OpenPages.IndexOf(page);
          if (fileManager.UserControls[pageIndex] is TextEditorUI textEditor)
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
      foreach (var userControl in fileManager.UserControls)
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
      searchText = Regex.Escape(searchText);
      return wholeWord == true ? $@"(?<!\w){searchText}(?!\w)" : searchText;
    }

    /// <summary>
    /// Обрабатывает найденные совпадения и заполняет коллекцию foundResults.
    /// </summary>
    private void ProcessMatches(MatchCollection matches)
    {
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

      string[] lines = fullText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

      for (int i = 0; i < lines.Length; i++)
      {
        string line = lines[i];
        if (line.Equals("    for (int i = 0; i < header.NumberOfSignals; i++)"))
        {
          Console.WriteLine("Дошли до строки с пробелами в начале");
        }
        MatchCollection matches = Regex.Matches(line, pattern, options);
        if (Regex.IsMatch(lines[i], pattern, options))
        {
          results.Add(new SearchResult(lineOffset, searchText.Length, i + 1, lines[i]));
        }
        lineOffset += line.Length + Environment.NewLine.Length;
      }

      return results.Count > 0 ? results : null;
    }

    /// <summary>
    /// Переход к следующему вхождению.
    /// </summary>
    private void NextOccurrence(int searchArea, TextEditorUI textEditor)
    {
      if (foundResults.Count > 0)
      {
        currentIndex++;
        textEditor.MarkerService.ClearAllMarkers();

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
      int currentIndex = fileManager.OpenPages.FindIndex(p => p.Background == (Brush)Application.Current.Resources["ActiveBorderSolidColorBrush"]);
      var nextIndex = currentIndex + 1;

      if (currentIndex >= 0 && currentIndex < fileManager.OpenPages.Count - 1)
      {
        var nextPage = fileManager.OpenPages[nextIndex];
        while (!(fileManager.UserControls[nextIndex] is TextEditorUI))
        {
          nextIndex++;
          nextPage = fileManager.OpenPages[nextIndex];
        }
        _textEditor++;
        Application.Current.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Render);
        ShowControl(fileManager.UserControls[nextIndex], nextPage);
        return _textEditor;
      }
      if (nextIndex > fileManager.OpenPages.Count - 1)
      {
        nextIndex = 0;
        _textEditor = -1;
        var nextPage = fileManager.OpenPages[nextIndex];
        while (!(fileManager.UserControls[nextIndex] is TextEditorUI))
        {
          nextIndex++;
          nextPage = fileManager.OpenPages[nextIndex];
        }
        if (nextIndex >= fileManager.OpenPages.Count)
        {
          return -1;
        }
        else
        {
          _textEditor++;
          ShowControl(fileManager.UserControls[nextIndex], nextPage);
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

      textEditor.MarkerService.ClearAllMarkers();
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
        var activeTab = fileManager.OpenPages.FirstOrDefault(page => page.Background == (Brush)Application.Current.Resources["ActiveBorderSolidColorBrush"]);
        int pageIndex = fileManager.OpenPages.IndexOf(activeTab);

        if (fileManager.UserControls[pageIndex] is TextEditorUI textEditor)
        {
          var result = foundResults[index];
          textEditor.MarkerService.ClearAllMarkers();
          textEditor.MarkerService.AddMarker(result.StartOffset, result.Length, (Color)ColorConverter.ConvertFromString("#b23a48"));
          textEditor.TextArea.TextView.InvalidateLayer(KnownLayer.Selection);
          int lineNumber = textEditor.Document.GetLineByOffset(result.StartOffset).LineNumber;
          textEditor.ScrollToLine(lineNumber);
          textEditor.TextArea.Caret.Offset = result.StartOffset + result.Length;
          textEditor.Focus();
          textEditor.TextArea.Focus();
          textEditor.TextArea.Caret.BringCaretToView();
        }
      }
    }

    /// <summary>
    /// Очистка подстветки.
    /// </summary>
    private void ClearHighlights(TextEditorUI textEditor)
    {
      textEditor?.MarkerService?.ClearAllMarkers();
      foundResults.Clear();
      textEditor.TextArea.SelectionBorder = null;
      currentIndex = -1;
    }


    /// <summary>
    /// Очистка подстветки.
    /// </summary>
    public void OnSearchWindowClosing()
    {
      foreach (var control in fileManager.UserControls)
      {
        if (control is TextEditorUI textEditor)
        {
          ClearHighlights(textEditor);
        }
      }
      _pendingHighlights.Clear();
      _fullText = new Dictionary<UserControl, string>();
      _wholeWord = false;
      _caseWord = false;
      _searchArea = 0;
      _searchParameters = string.Empty;
      hasChanged = true;
    }

    public void GetLineOccurrences(string fileName, int lineNumber, int startOffset, string lineText)
    {
      var foundPage = fileManager.OpenPages.FirstOrDefault(page => page.Text == fileName);
      if (foundPage == null)
        return;

      int pageIndex = fileManager.OpenPages.IndexOf(foundPage);

      if (fileManager.UserControls[pageIndex] is TextEditorUI textEditor)
      {
        if (textEditor.MarkerService == null)
        {
          Console.WriteLine("❌ markerService == null");
          return;
        }

        ShowControl(textEditor, foundPage);
        textEditor.MarkerService.ClearAllMarkers();
        var ranges = new List<(int start, int end)>();
        int offsetInDocument = startOffset;

        int index = 0;

        StringComparison options = _caseWord == true ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

        while ((index = lineText.IndexOf(_searchText, index, options)) >= 0)
        {
          int start = offsetInDocument + index;
          int end = start + _searchText.Length;
          ranges.Add((start, end));
          index += _searchText.Length;
        }

        textEditor.HighlightRanges(ranges);

        if (ranges.Count > 0)
        {
          textEditor.ScrollToLine(lineNumber);
        }

        textEditor.Focus();
      }
    }

    /// <summary>
    /// Отображает указанный элемент управления, скрывая остальные.
    /// </summary>
    /// <param name="control">Элемент управления для отображения.</param>
    private void ShowControl(UserControl control, OpenFileButton openPage)
    {
      var controlManager = new ControlManager(this.fileManager, this.multiEditorControl);
      controlManager.ShowControl(control, openPage);
    }

    public TextSearchManager(FileManager fileManager, MultiEditorControl multiEditorControl)
    {
      this.fileManager = fileManager;
      this.multiEditorControl = multiEditorControl;
    }
  }
}
