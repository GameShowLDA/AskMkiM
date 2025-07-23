using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AppConfiguration.Base;
using DevZest.Windows.Docking;
using ICSharpCode.AvalonEdit.Rendering;
using Message;
using UI.Components.SearchControls;
using UI.Controls.TextEditor;
using static Utilities.LoggerUtility;

namespace UI.Components.MultiEditorMethods
{
  /// <summary>
  /// Класс для работы с поиском по тексту.
  /// </summary>
  public class TextSearchManager
  {
    /// <summary>
    /// Экземпляр класса <see cref="FileManager"/> для управления файлами.
    /// </summary>
    private FileManager fileManager { get; set; }

    /// <summary>
    /// Экземпляр класса <see cref="MultiEditorControl"/> для управления мульти-редактором.
    /// </summary>
    private MultiEditorControl multiEditorControl { get; set; }

    /// <summary>
    /// Словарь, хранящий результаты поиска для каждого открытого файла.
    /// Ключ - имя файла, значение - список результатов поиска для этого файла.
    /// </summary>
    public Dictionary<string, List<SearchResult>> foundInOpenedFiles = new Dictionary<string, List<SearchResult>>();

    /// <summary>
    /// Список результатов поиска, полученных для текущего документа.
    /// </summary>
    private List<SearchResult> foundResults = new List<SearchResult>();

    /// <summary>
    /// Индекс текущего результата поиска, на который нужно перейти.
    /// </summary>
    private int currentIndex = -1;

    /// <summary>
    /// Текст для поиска.
    /// </summary>
    internal string _searchText;

    /// <summary>
    /// Словарь, который хранит полный текст для каждого элемента управления <see cref="UserControl"/>.
    /// Ключ - элемент управления, значение - полный текст.
    /// </summary>
    internal Dictionary<UserControl, string> _fullText = new Dictionary<UserControl, string>();

    /// <summary>
    /// Флаг, который указывает, нужно ли искать только целые слова (если значение true) или все вхождения (если false).
    /// </summary>
    internal bool? _wholeWord;

    /// <summary>
    /// Флаг, который указывает, следует ли учитывать регистр при поиске (если значение true) или нет (если false).
    /// </summary>
    internal bool? _caseWord;

    /// <summary>
    /// Параметры поиска, которые могут включать "FindNext", "FindPrevious" и другие режимы поиска.
    /// </summary>
    internal string _searchParameters;

    /// <summary>
    /// Область поиска. Определяет, будет ли поиск выполняться только в текущем документе или во всех открытых документах.
    /// </summary>
    internal int _searchArea;

    /// <summary>
    /// Индекс текущего текстового редактора.
    /// </summary>
    internal int _textEditor;

    /// <summary>
    /// Флаг, который указывает, были ли изменения в параметрах поиска, которые требуют повторного поиска.
    /// </summary>
    internal bool hasChanged;

    /// <summary>
    /// Словарь, хранящий данные для подсветки текста.
    /// </summary>
    private Dictionary<string, (int lineNumber, int lineLength)> _pendingHighlights = new Dictionary<string, (int lineNumber, int lineLength)>();

    /// <summary>
    /// Событие, которое вызывается, когда результаты поиска готовы для отображения.
    /// </summary>
    public event Action<string, bool?, Dictionary<string, List<SearchResult>>> SearchResultsReady;

    /// <summary>
    /// Выполняет поиск по тексту с учетом различных параметров.
    /// </summary>
    /// <param name="searchText">Текст, который мы ищем.</param>
    /// <param name="wholeWord">Если true - ищем только слово целиком, false - ищем все вхождения заданного текста.</param>
    /// <param name="caseWord">Если true - учитываем регистр, false - не учитываем.</param>
    /// <param name="searchArea">Параметры поиска: найти далее, найти предыдущее, найти все.</param>
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
        ClearOldHighlights(fullText);
      }

      InitializeSearch(fullText, searchText, wholeWord, caseWord, searchArea, searchParameters);

      if (hasChanged)
      {
        await ExecuteSearch(searchText, wholeWord, caseWord, searchArea, searchParameters, fullText);
      }
      else
      {
        HandleSearchNavigation(searchParameters, fullText);
      }
    }

    /// <summary>
    /// Очищает старые выделения в открытых файлах.
    /// </summary>
    /// <param name="fullText">Полный текст всех документов для поиска.</param>
    private void ClearOldHighlights(Dictionary<UserControl, string> fullText)
    {
      foreach (var key in fullText.Keys)
      {
        if (key is TextEditorUI textEditor)
        {
          ClearHighlights(textEditor);
        }
      }
    }

    /// <summary>
    /// Выполняет поиск в зависимости от параметров.
    /// </summary>
    /// <param name="searchText">Текст для поиска.</param>
    /// <param name="wholeWord">Флаг, указывающий, искать ли целые слова.</param>
    /// <param name="caseWord">Флаг, указывающий, учитывать ли регистр.</param>
    /// <param name="searchArea">Область поиска: по текущему документу или всем.</param>
    /// <param name="searchParameters">Параметры поиска, такие как "FindAll", "FindNext", "FindPrevious".</param>
    /// <param name="fullText">Полный текст всех документов для поиска.</param>
    private async Task ExecuteSearch(string searchText, bool? wholeWord, bool? caseWord, int searchArea, string searchParameters, Dictionary<UserControl, string> fullText)
    {
      if (searchParameters == "FindAll")
      {
        await FindAllAsync(searchText, wholeWord, caseWord, searchArea);
      }
      else
      {

        var activeTextEditorContainer = fileManager.UserControls.OfType<TextEditorContainer>().FirstOrDefault();
        if (activeTextEditorContainer != null)
        {
          var dockItems = activeTextEditorContainer.DockManager.DockItems;
          var index = dockItems.IndexOf(dockItems.FirstOrDefault(item => item.IsActiveItem == true));
          if (dockItems[index].Content is TextEditorUI)
          {
            var textEditor = dockItems[index].Content as TextEditorUI;
            var allOccurrences = FindAllOccurrences(textEditor.Text, searchText, wholeWord, caseWord, searchArea);

            if (allOccurrences != null)
            {
              InitializeCurrentIndex();
              GoToOccurrence(currentIndex);
            }
          }
        }
      }
    }

    /// <summary>
    /// Обрабатывает навигацию по результатам поиска.
    /// </summary>
    /// <param name="searchParameters">Параметры поиска: FindNext, FindPrevious или FindAll.</param>
    /// <param name="fullText">Полный текст всех документов для поиска.</param>
    private async void HandleSearchNavigation(string searchParameters, Dictionary<UserControl, string> fullText)
    {
      var activeTab = fileManager.OpenPages.FirstOrDefault(page => page.Background == (Brush)Application.Current.Resources["ActiveBorderSolidColorBrush"]);

      if (activeTab != null)
      {
        int pageIndex = fileManager.OpenPages.IndexOf(activeTab);

        if (fileManager.UserControls[pageIndex] is TextEditorContainer textEditorContainer)
        {
          var foundDockItem = textEditorContainer.DockManager.DockItems.FirstOrDefault(item => item.IsActiveItem == true);
          if (foundDockItem != null)
          {
            if (foundDockItem.Content is TextEditorUI textEditor)
            {
              switch (searchParameters)
              {
                case "FindNext":
                  NextOccurrence(_searchArea, textEditor);
                  break;

                case "FindPrevious":
                  PreviousOccurrence(_searchArea, textEditor);
                  break;

                case "FindAll":
                  await FindAllAsync(_searchText, _wholeWord, _caseWord, _searchArea);
                  break;
              }
            }
          }
        }
      }
    }

    /// <summary>
    /// Выполняет асинхронный поиск по всем открытым документам.
    /// </summary>
    /// <param name="searchText">Текст для поиска.</param>
    /// <param name="wholeWord">Флаг, указывающий, нужно ли искать только целые слова.</param>
    /// <param name="caseWord">Флаг, указывающий, учитывать ли регистр.</param>
    /// <param name="searchArea">Параметры поиска: текущий документ или все документы.</param>
    public async Task FindAllAsync(string searchText, bool? wholeWord, bool? caseWord, int searchArea, bool showResults = true)
    {
      if (string.IsNullOrEmpty(searchText))
      {
        ShowEmptySearchWarning();
        return;
      }

      EventAggregator.RaiseRequestShowProgress();

      List<DockItem> searchPages = SetSearchAreaPages(searchArea);
      ClearPreviousSearchResults();

      List<Task> searchTasks = ExecuteSearchTasks(searchPages, searchText, wholeWord, caseWord);
      await Task.WhenAll(searchTasks);

      if (showResults)
      {
        HandleSearchResults(searchText);
      }
      else
      {
        EventAggregator.RaiseRequestCloseProgress();
      }
    }

    /// <summary>
    /// Показывает предупреждение, если поле для поиска пустое.
    /// </summary>
    private void ShowEmptySearchWarning()
    {
      MessageBoxCustom.Show("Введите текст для поиска.", image: MessageBoxImage.Warning);
      LogWarning("Поле для поиска не заполнено");
    }

    /// <summary>
    /// Очищает результаты предыдущих поисков.
    /// </summary>
    private void ClearPreviousSearchResults()
    {
      if (foundInOpenedFiles.Count > 0)
      {
        foundInOpenedFiles.Clear();
      }
    }

    /// <summary>
    /// Обрабатывает результаты поиска после завершения поиска.
    /// </summary>
    /// <param name="searchText">Текст поиска.</param>
    private void HandleSearchResults(string searchText)
    {
      if (foundInOpenedFiles.Count > 0)
      {
        var lastFoundResultsDictionary = foundInOpenedFiles.Values.LastOrDefault();
        if (lastFoundResultsDictionary?.Count > 0)
        {
          DisplaySearchResults(searchText, _caseWord, foundInOpenedFiles);
          EventAggregator.RaiseRequestCloseProgress();
        }
      }
      else
      {
        EventAggregator.RaiseRequestCloseProgress();
        MessageBoxCustom.Show("Текст не найден в открытых документах.", image: MessageBoxImage.Warning);
        LogInformation("Текст не найден в открытых документах.");
      }
    }

    /// <summary>
    /// Выполняет поиск в отдельных задачах для каждого открытого документа.
    /// </summary>
    /// <param name="searchPages">Список страниц для поиска.</param>
    /// <param name="searchText">Текст для поиска.</param>
    /// <param name="wholeWord">Флаг поиска целых слов.</param>
    /// <param name="caseWord">Флаг чувствительности к регистру.</param>
    /// <returns>Список задач поиска.</returns>
    private List<Task> ExecuteSearchTasks(List<DockItem> searchPages, string searchText, bool? wholeWord, bool? caseWord)
    {
      object lockObj = new object();

      // Сначала получаем нужные данные на UI-потоке
      var searchData = searchPages.Select(page =>
      {
        string pageName = page.Dispatcher.Invoke(() => page.Title);
        string pageText = null;
        if (page.Dispatcher.Invoke(() => page.Content) is TextEditorUI textEditor)
        {
          pageText = textEditor.Dispatcher.Invoke(() => textEditor.Text);
        }
        return new { pageName, pageText };
      }).Where(x => x.pageText != null).ToList();

      // Потом параллельно ищем
      return searchData.Select(data => Task.Run(() =>
      {
        var foundResultsList = FindOccurrencesByLine(data.pageText, searchText, wholeWord, caseWord);
        if (foundResultsList != null && foundResultsList.Count > 0)
        {
          lock (lockObj)
          {
            foundInOpenedFiles[data.pageName] = foundResultsList;
          }
        }
      })).ToList();
    }


    /// <summary>
    /// Задает страницы, в которых необходимо провести поиск по тексту.
    /// </summary>
    /// <param name="searchArea">Область поиска текста.</param>
    /// <returns>Коллекцию вкладок, в которых находятся текстовые редакторы, где необходимо провести поиск по тексту.</returns>
    private List<DockItem> SetSearchAreaPages(int searchArea)
    {
      var activeTextEditorContainer = fileManager.UserControls.OfType<TextEditorContainer>().FirstOrDefault();
      var searchPages = new List<DockItem>();
      if (searchArea == 0 || searchArea == 2)
      {
        //var activeTab = fileManager.OpenPages.FirstOrDefault(page => page.Background == (Brush)Application.Current.Resources["ActiveBorderSolidColorBrush"]);
        //var pageIndex = fileManager.OpenPages.IndexOf(activeTab);
        //searchPages.Add(fileManager.OpenPages[pageIndex]);
        if (activeTextEditorContainer != null)
        {
          var dockItems = activeTextEditorContainer.DockManager.DockItems;
          var index = dockItems.IndexOf(dockItems.FirstOrDefault(item => item.IsActiveItem == true));
          if (dockItems[index].Content is TextEditorUI)
          {
            searchPages.Add(dockItems[index]);
          }
        }
      }
      else
      {
        foreach (var item in activeTextEditorContainer.DockManager.DockItems)
        {
          if (item.Content.GetType() == typeof(TextEditorUI))
          {
            searchPages.Add(item);
          }
        }
      }

      return searchPages;
    }

    /// <summary>
    /// Отображает результаты поиска в DataGrid.
    /// </summary>
    /// <param name="searchText">Искомый текст.</param>
    /// <param name="isCaseSensetive">Значение, определяющее нужно учитывать регистр или нет.</param>
    /// <param name="results">Результаты поиска.</param>
    public void DisplaySearchResults(string searchText, bool? isCaseSensetive, Dictionary<string, List<SearchResult>> results)
    {
      if (results == null || results.Count == 0)
      {
        MessageBoxCustom.Show("Результаты поиска пусты!", "Ошибка", MessageBoxButton.OK, image: MessageBoxImage.Warning);
        return;
      }

      OnSearchResultsReady(searchText, isCaseSensetive, results);
    }

    private void OnSearchResultsReady(string searchText, bool? isCaseSensitive, Dictionary<string, List<SearchResult>> results)
    {
      SearchResultsReady?.Invoke(searchText, isCaseSensitive, results);
    }

    /// <summary>
    /// Получает полный текст всех открытых документов в зависимости от области поиска.
    /// </summary>
    /// <param name="searchArea">Область поиска: 0 - текущий документ, 1 - все открытые документы, 2 - только активный документ.</param>
    /// <returns>Словарь, в котором ключ - элемент управления, а значение - текст из редактора.</returns>
    public Dictionary<UserControl, string> GetText(int searchArea)
    {
      Dictionary<UserControl, string> fullText = new Dictionary<UserControl, string>();

      if (searchArea == 0 || searchArea == 2)
      {
        var textEditorContainer = fileManager.UserControls.OfType<TextEditorContainer>().FirstOrDefault();
        var activeDockItem = textEditorContainer.DockManager.DockItems.FirstOrDefault(item => item.IsActiveItem == true && item.Content.GetType() == typeof(TextEditorUI));
        if (activeDockItem != null)
        {
          var textEditor = activeDockItem.Content as TextEditorUI;
          fullText.Add(textEditor, textEditor.Text);
        }
      }
      else
      {
        AddTextFromAllDocuments(fullText);
      }

      return fullText;
    }

    /// <summary>
    /// Добавляет текст всех открытых документов в словарь.
    /// </summary>
    /// <param name="fullText">Словарь для хранения текста.</param>
    private void AddTextFromAllDocuments(Dictionary<UserControl, string> fullText)
    {
      var textEditorContainer = fileManager.UserControls.OfType<TextEditorContainer>().FirstOrDefault();

      foreach (var item in textEditorContainer.DockManager.DockItems)
      {
        if (item.Content is TextEditorUI textEditor)
        {
          if (!fullText.ContainsKey(textEditor))
          {
            fullText.Add(textEditor, textEditor.Text);
          }
        }
      }
    }

    /// <summary>
    /// Инициализирует параметры поиска по тектсу.
    /// </summary>
    /// <param name="searchText">Текст, который мы ищем.</param>
    /// <param name="wholeWord">Если true - ищем только слово целиком, false - ищем все вхождения заданного текста.</param>
    /// <param name="caseWord">Если true - учитываем регистр, false - не учитываем.</param>
    /// <param name="searchArea">Параметры поиска: найти  далее, найти предыдущее, найти все.</param>
    /// <param name="searchParameters">Область поиска: поиск в текущем документе, во всех открытых документах, в файле.</param>
    public void InitializeSearch(Dictionary<UserControl, string> fullText, string searchText, bool? wholeWord, bool? caseWord, int searchArea, string searchParameters)
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

    /// <summary>
    /// Ищет все вхождения текста в строке с учётом параметров поиска.
    /// </summary>
    /// <param name="fullText">Полный текст для поиска.</param>
    /// <param name="searchText">Искомый текст.</param>
    /// <param name="wholeWord">Если true, ищет только полные слова, если false, ищет все вхождения.</param>
    /// <param name="caseWord">Если true, ищет с учётом регистра, если false — без учёта.</param>
    /// <param name="searchArea">Определяет область поиска (например, текущий документ или все документы).</param>
    /// <returns>Список результатов поиска или null, если текст не найден.</returns>
    public List<SearchResult> FindAllOccurrences(string fullText, string searchText, bool? wholeWord, bool? caseWord, int searchArea)
    {
      ClearAllHighlights();

      if (!ValidateSearchText(searchText))
      {
        return null;
      }

      string pattern = BuildRegexPattern(searchText, wholeWord);
      RegexOptions options = GetRegexOptions(caseWord);

      MatchCollection matches = FindMatches(fullText, pattern, options);
      ProcessMatches(matches);

      EventAggregator.RaiseInfoMessage($"Найдено {foundResults.Count} вхождений");
      return foundResults.Count > 0 ? foundResults : HandleNoMatches(searchText);
    }

    /// <summary>
    /// Проверяет, является ли текст для поиска валидным (не пустым).
    /// </summary>
    /// <param name="searchText">Текст для поиска.</param>
    /// <returns>True, если текст валиден, иначе False.</returns>
    private bool ValidateSearchText(string searchText)
    {
      if (string.IsNullOrEmpty(searchText))
      {
        MessageBoxCustom.Show("Введите текст для поиска.", image: MessageBoxImage.Warning);
        LogWarning("Поле для поиска не заполнено");
        return false;
      }

      return true;
    }

    /// <summary>
    /// Формирует регулярное выражение для поиска.
    /// </summary>
    /// <param name="searchText">Текст для поиска.</param>
    /// <param name="wholeWord">Если true, ищет только полные слова.</param>
    /// <returns>Регулярное выражение для поиска.</returns>
    private string BuildRegexPattern(string searchText, bool? wholeWord)
    {
      searchText = Regex.Escape(searchText);
      return wholeWord == true ? $@"\b{searchText}\b" : searchText;
    }

    /// <summary>
    /// Определяет опции для регулярного выражения в зависимости от чувствительности к регистру.
    /// </summary>
    /// <param name="caseWord">Флаг, указывающий на чувствительность к регистру.</param>
    /// <returns>Опции для регулярного выражения.</returns>
    private RegexOptions GetRegexOptions(bool? caseWord)
    {
      return caseWord == true ? RegexOptions.None : RegexOptions.IgnoreCase;
    }

    /// <summary>
    /// Находит все совпадения для регулярного выражения в тексте.
    /// </summary>
    /// <param name="fullText">Текст для поиска.</param>
    /// <param name="pattern">Регулярное выражение для поиска.</param>
    /// <param name="options">Опции для регулярного выражения.</param>
    /// <returns>Коллекция найденных совпадений.</returns>
    private MatchCollection FindMatches(string fullText, string pattern, RegexOptions options)
    {
      return Regex.Matches(fullText, pattern, options);
    }

    /// <summary>
    /// Обрабатывает найденные совпадения и сохраняет их в результирующий список.
    /// </summary>
    /// <param name="matches">Коллекция найденных совпадений.</param>
    private void ProcessMatches(MatchCollection matches)
    {
      foundResults.Clear();
      foreach (Match match in matches)
      {
        foundResults.Add(new SearchResult(match.Index, match.Length));
      }
    }

    /// <summary>
    /// Обрабатывает случай, когда совпадения не найдены.
    /// </summary>
    /// <param name="searchText">Искомый текст.</param>
    /// <returns>Возвращает null и выводит сообщение о том, что текст не найден.</returns>
    private List<SearchResult> HandleNoMatches(string searchText)
    {
      MessageBoxCustom.Show($"Текст {searchText} не найден.", image: MessageBoxImage.Error);
      LogInformation($"Текст {searchText} не найден.");
      return null;
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
          currentIndex = foundResults.Count - 1;
        }
      }
    }

    /// <summary>
    /// Ищет все вхождения текста в каждой строке текста, с учётом параметров поиска.
    /// </summary>
    /// <param name="fullText">Полный текст для поиска.</param>
    /// <param name="searchText">Искомый текст.</param>
    /// <param name="wholeWord">Если true, ищет только полные слова, если false, ищет все вхождения.</param>
    /// <param name="caseWord">Если true, учитывается регистр, если false — не учитывается.</param>
    /// <param name="lineOffset">Смещение в тексте для вычисления позиции в строках.</param>
    /// <returns>Список результатов поиска или null, если текст не найден.</returns>
    public List<SearchResult> FindOccurrencesByLine(string fullText, string searchText, bool? wholeWord, bool? caseWord, int startOffset = 0)
    {
      var results = new List<SearchResult>();
      if (string.IsNullOrEmpty(searchText))
        return results;

      RegexOptions options = caseWord == true ? RegexOptions.None : RegexOptions.IgnoreCase;
      string escapedSearchText = Regex.Escape(searchText);
      string pattern = wholeWord == true ? $@"\b{escapedSearchText}\b" : escapedSearchText;

      // Используем регэксп, чтобы не терять реальные line ending
      var lineMatches = Regex.Matches(fullText, @"([^\r\n]*)(\r\n|\n|\r)?");
      int offset = startOffset;
      int lineNumber = 1;

      foreach (Match lineMatch in lineMatches)
      {
        string line = lineMatch.Groups[1].Value;
        string lineEnding = lineMatch.Groups[2].Value;

        // Поиск совпадений в строке
        MatchCollection matches = Regex.Matches(line, pattern, options);
        foreach (Match match in matches)
        {
          int globalStart = offset + match.Index;
          LogDebug($"Найдено вхождение: {match.Value} на строке {lineNumber}, позиция {globalStart}");
          // Для отладки:
          if (globalStart < fullText.Length)
          {
            LogDebug($"Подстрока до позиции {globalStart}: {fullText.Substring(0, globalStart)}");
            int endPosition = globalStart + searchText.Length;
            int afterLength = Math.Min(fullText.Length - endPosition, 50); // максимум 50 символов для отладки
            LogDebug($"Подстрока после слова {match.Value}: {fullText.Substring(endPosition, afterLength)}");
          }

          results.Add(new SearchResult(globalStart, match.Length, lineNumber, match.Value));
        }

        offset += line.Length + lineEnding.Length;
        lineNumber++;
      }

      return results;
    }




    /// <summary>
    /// Переход к следующему вхождению.
    /// </summary>
    /// <param name="searchArea">Область поиска.</param>
    /// <param name="textEditor">Текстовый редактор.</param>
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
              MessageBoxCustom.Show("Достигнуто последнее совпадение. Дополнительных вхождений в тексте нет.", "Поиск закончен", image: MessageBoxImage.Warning);
              return;
            }
          }

          currentIndex = 0;
        }

        GoToOccurrence(currentIndex);
      }
      else
      {
        MessageBoxCustom.Show($"Текст {_searchText} не найден", "Ошибка", image: MessageBoxImage.Error);
        return;
      }
    }

    /// <summary>
    /// Переходит к следующему документу в списке открытых вкладок.
    /// </summary>
    /// <returns>Индекс следующего документа, или -1, если переход невозможен.</returns>
    private int SwitchToNextDocument()
    {
      int currentIndex = GetCurrentIndex();
      var textEditorContainer = fileManager.UserControls.OfType<TextEditorContainer>().FirstOrDefault();

      if (currentIndex >= 0 && currentIndex < textEditorContainer.DockManager.DockItems.Count - 1)
      {
        int nextIndex = GetNextValidIndex(currentIndex + 1);

        if (nextIndex >= 0)
        {
          ShowNextPage(nextIndex);
          return nextIndex;
        }
      }
      else
      {
        int nextIndex = GetNextValidIndex(0);
        if (nextIndex >= 0)
        {
          ShowNextPage(nextIndex);
          return nextIndex;
        }
      }

      return -1;
    }

    /// <summary>
    /// Получает индекс текущей активной вкладки.
    /// </summary>
    /// <returns>Индекс текущей активной вкладки, или -1 если активная вкладка не найдена.</returns>
    private int GetCurrentIndex()
    {
      var textEditroContainer = fileManager.UserControls.OfType<TextEditorContainer>().FirstOrDefault();
      if (textEditroContainer != null)
      {
        var dockItems = textEditroContainer.DockManager.DockItems;
        return dockItems.IndexOf(dockItems.FirstOrDefault(item => item.IsActiveItem == true));
      }
      return -1;
    }

    /// <summary>
    /// Находит индекс следующей вкладки, которая является текстовым редактором.
    /// </summary>
    /// <param name="startIndex">Индекс, с которого начинаем искать следующую вкладку.</param>
    /// <returns>Индекс следующей вкладки с текстовым редактором, или -1, если таковой не найден.</returns>
    private int GetNextValidIndex(int startIndex)
    {
      // TODO: придумать как обнулять счетчик и потом снова его запускать

      var textEditroContainer = fileManager.UserControls.OfType<TextEditorContainer>().FirstOrDefault();
      if (textEditroContainer != null)
      {
        var dockItems = textEditroContainer.DockManager.DockItems;
        for (int i = startIndex; i < dockItems.Count; i++)
        {
          if (dockItems[i].Content is TextEditorUI)
          {
            return i;
          }
        }
      }

      return -1;
    }

    /// <summary>
    /// Показывает следующую вкладку и обновляет её отображение.
    /// </summary>
    /// <param name="nextIndex">Индекс следующей вкладки, которую нужно отобразить.</param>
    private void ShowNextPage(int nextIndex)
    {
      var textEditroContainer = fileManager.UserControls.OfType<TextEditorContainer>().FirstOrDefault();
      if (textEditroContainer != null)
      {
        var dockItems = textEditroContainer.DockManager.DockItems;
        dockItems[nextIndex].Show(textEditroContainer.DockManager);
      }
    }

    /// <summary>
    /// Переход к предыдущему вхождению.
    /// </summary>
    private void PreviousOccurrence(int searchArea, TextEditorUI textEditor)
    {
      if (foundResults.Count == 0)
      {
        return;
      }

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

        var textEditroContainer = fileManager.UserControls.FirstOrDefault(editor => editor.GetType() == typeof(TextEditorContainer));
        if (textEditroContainer != null)
        {
          var activeDockItem = (textEditroContainer as TextEditorContainer).DockManager.DockItems.FirstOrDefault(item => item.IsActiveItem == true);
          if (activeDockItem != null)
          {
            if (activeDockItem.Content is TextEditorUI textEditor)
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
      var textEditorContainer = fileManager.UserControls.OfType<TextEditorContainer>().FirstOrDefault();
      if (textEditorContainer != null)
      {

        foreach (var control in textEditorContainer.DockManager.DockItems)
        {
          if (control.Content is TextEditorUI textEditor)
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
    }

    /// <summary>
    /// Находит и подсвечивает все вхождения искомого текста в указанной строке.
    /// </summary>
    /// <param name="fileName">Имя файла, в котором нужно найти вхождения.</param>
    /// <param name="lineNumber">Номер строки для подсветки в редакторе.</param>
    /// <param name="startOffset">Смещение в документе, с которого начинается поиск.</param>
    /// <param name="lineText">Текст строки для поиска вхождений.</param>
    public void GetLineOccurrences(string fileName, int lineNumber, int startOffset, string lineText)
    {
      var foundPage = FindFilePage();
      if (foundPage == null)
      {
        return;
      }

      var foundDockItem = foundPage.DockManager.DockItems.FirstOrDefault(item => item.Title == fileName);
      if (foundDockItem.Content is TextEditorUI textEditor)
      {
        if (textEditor.MarkerService == null)
        {
          LogError("markerService == null");
          return;
        }

        foundDockItem.Show(foundPage.DockManager);
        //ShowControl(textEditor, foundPage);
        textEditor.MarkerService.ClearAllMarkers();

        var ranges = FindAllOccurrencesInLine(lineText, startOffset);

        textEditor.HighlightRanges(ranges);

        if (ranges.Count > 0)
        {
          textEditor.ScrollToLine(lineNumber);
        }

        textEditor.Focus();
      }
    }

    /// <summary>
    /// Находит страницу по имени файла.
    /// </summary>
    /// <param name="fileName">Имя файла для поиска страницы.</param>
    /// <returns>Страница с данным файлом, или null, если не найдена.</returns>
    private TextEditorContainer FindFilePage()
    {
      return fileManager.UserControls.OfType<TextEditorContainer>().FirstOrDefault();
    }

    /// <summary>
    /// Находит все вхождения искомого текста в строке и возвращает их диапазоны.
    /// </summary>
    /// <param name="lineText">Текст строки для поиска вхождений.</param>
    /// <param name="startOffset">Смещение для первого вхождения в документе.</param>
    /// <returns>Список диапазонов (начало, конец) для всех вхождений текста.</returns>
    private List<(int start, int end)> FindAllOccurrencesInLine(string lineText, int startOffset)
    {
      var ranges = new List<(int start, int end)>();
      int index = 0;

      StringComparison options = _caseWord == true ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

      while ((index = lineText.IndexOf(_searchText, index, options)) >= 0)
      {
        int start = startOffset + index;
        int end = start + _searchText.Length;
        ranges.Add((start, end));
        index += _searchText.Length;
      }

      return ranges;
    }

    /// <summary>
    /// Конструктор, для инициализации <see cref="TextSearchManager"/>.
    /// </summary>
    /// <param name="fileManager">Экземпляр класса <see cref="FileManager"/>.</param>
    /// <param name="multiEditorControl">Экземпляр <see cref="MultiEditorControl"/> для взаимодействия с редактором.</param>
    public TextSearchManager(FileManager fileManager, MultiEditorControl multiEditorControl)
    {
      this.fileManager = fileManager;
      this.multiEditorControl = multiEditorControl;
    }

    /// <summary>
    /// Конструктор, для инициализации <see cref="TextSearchManager"/>.
    /// </summary>
    /// <param name="fileManager">Экземпляр класса <see cref="FileManager"/>.</param>
    /// <param name="multiEditorControl">Экземпляр <see cref="MultiEditorControl"/> для взаимодействия с редактором.</param>
    /// <param name="searchText">Искомый текст.</param>
    public TextSearchManager(FileManager fileManager, MultiEditorControl multiEditorControl, string searchText)
      : this(fileManager, multiEditorControl)
    {
      this._searchText = searchText;
    }
  }
}
