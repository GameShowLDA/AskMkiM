using Ask.Core.Services.EventCore.Adapters;
using ICSharpCode.AvalonEdit.Rendering;
using Ask.Engine.ControlCommandAnalyser.Model;
using Message;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using UI.Components.Invoke;
using UI.Components.SearchControls;
using UI.Controls;
using UI.Controls.TextEditor;
using UI.Windows.WpfDocking.Windows.Docking;
using static Ask.LogLib.LoggerUtility;
using Ask.Core.Shared.DTO.Executor;

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
    /// Список результатов поиска, полученных для текущего документа.
    /// </summary>
    private List<SearchResult> foundTranslatorResults = new List<SearchResult>();

    /// <summary>
    /// Индекс текущего результата поиска, на который нужно перейти.
    /// </summary>
    private int currentTranslatorIndex = -1;

    private static readonly Color SearchHighlightColor = (Color)ColorConverter.ConvertFromString("#b23a48");
    private const string SourceCommandHeaderPattern = @"^\s*(?<num>\d+(?:\s+\d+)*)\s+(?<mnemo>[А-ЯЁA-Z]{1,})\b";

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
          ClearHighlights(textEditor, foundResults, ref currentIndex);
        }
        else if (key is TranslatorItem item)
        {
          var leftTextEditor = item.GetLeftEditor();
          var rightTranslator = item.GetRightEditor();
          ClearHighlights(leftTextEditor, foundResults, ref currentIndex);
          ClearHighlights(rightTranslator, foundTranslatorResults, ref currentTranslatorIndex);
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
        var activeTab = fileManager.EditorWorkspaceModel.OpenPages.FirstOrDefault(
          page => page.Background == (Brush)Application.Current.Resources["ActiveBorderSolidColorBrush"]);

        if (activeTab != null)
        {
          int pageIndex = fileManager.EditorWorkspaceModel.OpenPages.IndexOf(activeTab);

          if (fileManager.EditorWorkspaceModel.UserControls[pageIndex] is TextEditorContainer textEditorContainer)
          {
            var foundDockItem = textEditorContainer.DockManager.DockItems.FirstOrDefault(item => item.IsActiveItem == true);
            if (foundDockItem != null)
            {
              TextEditorUI textEditor = new TextEditorUI();
              if (foundDockItem.Content is TextEditorUI)
              {
                textEditor = foundDockItem.Content as TextEditorUI;
              }
              else if (foundDockItem.Content is TranslatorItem translatorItem)
              {
                textEditor = translatorItem.GetLeftEditor();
              }
              else
              {
                return;
              }
              var allOccurrences = FindAllOccurrences(textEditor.Text, searchText, wholeWord, caseWord, searchArea);
              

              if (allOccurrences != null)
              {
                currentIndex = -1; // сброс перед вычислением позиции от каретки
                SetCurrentIndexFromCaret(textEditor);
                InitializeCurrentIndex();
                GoToOccurrence(currentIndex);
              }
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
      var activeTab = fileManager.EditorWorkspaceModel.OpenPages.FirstOrDefault(page => page.Background == (Brush)Application.Current.Resources["ActiveBorderSolidColorBrush"]);

      if (activeTab != null)
      {
        int pageIndex = fileManager.EditorWorkspaceModel.OpenPages.IndexOf(activeTab);

        if (fileManager.EditorWorkspaceModel.UserControls[pageIndex] is TextEditorContainer textEditorContainer)
        {
          var foundDockItem = textEditorContainer.DockManager.DockItems.FirstOrDefault(item => item.IsActiveItem == true);
          if (foundDockItem != null)
          {
            TextEditorUI textEditor = new TextEditorUI();
            if (foundDockItem.Content is TextEditorUI)
            {
              textEditor = foundDockItem.Content as TextEditorUI;
            }
            else if (foundDockItem.Content is TranslatorItem translatorItem)
            {
              textEditor = translatorItem.GetLeftEditor();
            }
            else
            {
              return;
            }
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

      Dictionary<DockItem, OpenFileButton> searchPages = SetSearchAreaPages(searchArea);
      ClearPreviousSearchResults();

      List<Task> searchTasks = ExecuteSearchTasks(searchPages, searchText, wholeWord, caseWord);
      await Task.WhenAll(searchTasks);

      if (showResults)
      {
        HandleSearchResults(searchText);
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
        }
      }
      else
      {
        MessageBoxCustom.Show("Текст не найден в открытых документах.", image: MessageBoxImage.Warning);
        LogInformation("Текст не найден в открытых документах.");
      }
    }

    /// <summary>
    /// Выполняет поиск в отдельных задачах для каждого открытого документа.
    /// </summary>
    /// <param name="searchPages">Словарь страниц для поиска и соответствующих им вкладок контейнеров.</param>
    /// <param name="searchText">Текст для поиска.</param>
    /// <param name="wholeWord">Флаг поиска целых слов.</param>
    /// <param name="caseWord">Флаг чувствительности к регистру.</param>
    /// <returns>Список задач поиска.</returns>
    private List<Task> ExecuteSearchTasks(Dictionary<DockItem, OpenFileButton> searchPages, string searchText, bool? wholeWord, bool? caseWord)
    {
      if (searchPages == null || searchPages.Count == 0)
      {
        return new List<Task>();
      }

      object lockObj = new object();

      // Сначала получаем нужные данные на UI-потоке
      var searchData = searchPages.Keys.Select(page =>
      {
        string pageName = page.Dispatcher.Invoke(() => page.Title);
        string pageText = null;
        if (page.Dispatcher.Invoke(() => page.Content) is TextEditorUI textEditor)
        {
          pageText = textEditor.Dispatcher.Invoke(() => textEditor.Text);
        }
        else if (page.Dispatcher.Invoke(() => page.Content) is TranslatorItem translatorItem)
        {
          pageText = translatorItem.Dispatcher.Invoke(() => translatorItem.GetLeftEditor().Document.Text);
        }
        else
        {
          pageText = null;
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
    /// <returns>
    /// Словарь, где ключ — вкладка редактора, а значение — родительская вкладка контейнера.
    /// </returns>
    private Dictionary<DockItem, OpenFileButton> SetSearchAreaPages(int searchArea)
    {
      var searchPages = new Dictionary<DockItem, OpenFileButton>();
      var openPages = fileManager.EditorWorkspaceModel.OpenPages;
      var userControls = fileManager.EditorWorkspaceModel.UserControls;

      if (openPages == null || userControls == null || openPages.Count == 0 || userControls.Count == 0)
      {
        return searchPages;
      }

      if (searchArea == 0 || searchArea == 2)
      {
        var activeTab = openPages.FirstOrDefault(
          page => page.Background == (Brush)Application.Current.Resources["ActiveBorderSolidColorBrush"]);

        if (activeTab == null)
        {
          return searchPages;
        }

        int activeIndex = openPages.IndexOf(activeTab);
        if (activeIndex < 0 || activeIndex >= userControls.Count || userControls[activeIndex] is not TextEditorContainer activeTextEditorContainer)
        {
          return searchPages;
        }

        var activeDockItem = activeTextEditorContainer.DockManager.DockItems.FirstOrDefault(item => item.IsActiveItem == true
          && (item.Content is TextEditorUI || item.Content is TranslatorItem));
        if (activeDockItem != null)
        {
          searchPages[activeDockItem] = activeTab;
        }

        return searchPages;
      }

      for (int i = 0; i < openPages.Count && i < userControls.Count; i++)
      {
        if (userControls[i] is not TextEditorContainer textEditorContainer)
        {
          continue;
        }

        foreach (var item in textEditorContainer.DockManager.DockItems)
        {
          if (item.Content is TextEditorUI || item.Content is TranslatorItem)
          {
            searchPages[item] = openPages[i];
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
        var activeTab = fileManager.EditorWorkspaceModel.OpenPages.FirstOrDefault(page => page.Background == (Brush)Application.Current.Resources["ActiveBorderSolidColorBrush"]);
        if (activeTab != null && fileManager.EditorWorkspaceModel.UserControls[fileManager.EditorWorkspaceModel.OpenPages.IndexOf(activeTab)] is TextEditorContainer textEditorContainer)
        {
          var activeDockItem = textEditorContainer.DockManager.DockItems.FirstOrDefault(item => item.IsActiveItem == true
          && (item.Content.GetType() == typeof(TextEditorUI) || item.Content.GetType() == typeof(TranslatorItem)));
          if (activeDockItem != null)
          {
            if (activeDockItem.Content is TextEditorUI textEditor)
            {
              fullText.Add(textEditor, textEditor.Text);
            }
            else if (activeDockItem.Content is TranslatorItem translatorItem)
            {
              var foundTextEditor = translatorItem.GetLeftEditor();
              fullText.Add(translatorItem, foundTextEditor.Text);
            }
          }
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
      var activeTab = fileManager.EditorWorkspaceModel.OpenPages.FirstOrDefault(page => page.Background == (Brush)Application.Current.Resources["ActiveBorderSolidColorBrush"]);
      if (activeTab != null && fileManager.EditorWorkspaceModel.UserControls[fileManager.EditorWorkspaceModel.OpenPages.IndexOf(activeTab)] is TextEditorContainer textEditorContainer)
      {
        foreach (var item in textEditorContainer.DockManager.DockItems)
        {
          if (item.Content is TextEditorUI textEditor)
          {
            if (!fullText.ContainsKey(textEditor))
            {
              fullText.Add(textEditor, textEditor.Text);
            }
          }
          else if (item.Content is TranslatorItem translatorItem)
          {
            var translator = translatorItem.GetLeftEditor();
            if (!fullText.ContainsKey(translatorItem))
            {
              fullText.Add(translatorItem, translator.Text);
            }
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

      MessageEventAdapter.RaiseInfoMessage($"Найдено {foundResults.Count} вхождений");
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
      foreach (var userControl in fileManager.EditorWorkspaceModel.UserControls)
      {
        if (userControl is TextEditorUI textEditor)
        {
          ClearHighlights(textEditor);
        }
        else if (userControl is TextEditorContainer textEditorContainer)
        {
          foreach (var item in textEditorContainer.DockManager.DockItems)
          {
            if (item.Content is TextEditorUI textEditorUI)
            {
              ClearHighlights(textEditorUI);
            }
            else if (item.Content is TranslatorItem translatorItem)
            {
              ClearHighlights(translatorItem.GetLeftEditor());
              ClearHighlights(translatorItem.GetRightEditor());
            }
          }
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
    /// Устанавливает currentIndex так, чтобы поиск начинался от позиции каретки.
    /// </summary>
    private void SetCurrentIndexFromCaret(TextEditorUI textEditor)
    {
      if (textEditor?.TextArea?.Caret == null || foundResults.Count == 0)
      {
        return;
      }

      int caretOffset = textEditor.TextArea.Caret.Offset;

      if (_searchParameters == "FindNext")
      {
        int idx = foundResults.FindIndex(r => r.StartOffset >= caretOffset);
        currentIndex = idx >= 0 ? idx : 0; // если дальше нет совпадений — переходим к первому
      }
      else if (_searchParameters == "FindPrevious")
      {
        int idx = foundResults.FindLastIndex(r => r.StartOffset < caretOffset);
        currentIndex = idx >= 0 ? idx : foundResults.Count - 1; // если левее нет — переходим к последнему
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
      if (foundResults.Count == 0)
      {
        MessageBoxCustom.Show($"Текст {_searchText} не найден", "Ошибка", image: MessageBoxImage.Error);
        return;
      }

      textEditor?.MarkerService?.ClearAllMarkers();

      int nextIndex = currentIndex + 1;
      if (nextIndex < foundResults.Count)
      {
        currentIndex = nextIndex;
        GoToOccurrence(currentIndex);
        return;
      }

      if (searchArea == 1)
      {
        if (TrySwitchDocumentWithMatches(moveForward: true))
        {
          return;
        }

        MessageBoxCustom.Show("Достигнуто последнее совпадение. Дополнительных вхождений в тексте нет.", "Поиск закончен", image: MessageBoxImage.Warning);
        return;
      }

      currentIndex = 0;
      GoToOccurrence(currentIndex);
    }

    /// <summary>
    /// Переходит к следующему/предыдущему документу и выбирает в нём первое/последнее совпадение.
    /// </summary>
    /// <param name="moveForward">
    /// true — переход вперёд к следующему документу;
    /// false — переход назад к предыдущему документу.
    /// </param>
    /// <returns>true, если удалось перейти к документу с совпадениями; иначе false.</returns>
    private bool TrySwitchDocumentWithMatches(bool moveForward)
    {
      var targets = GetSearchNavigationTargets();
      if (targets.Count == 0)
      {
        return false;
      }

      int activeTargetIndex = GetActiveSearchTargetIndex(targets);

      if (moveForward)
      {
        int startIndex = activeTargetIndex >= 0 ? activeTargetIndex + 1 : 0;
        for (int i = startIndex; i < targets.Count; i++)
        {
          if (TryActivateTargetAndGoToOccurrence(targets[i], moveForward))
          {
            return true;
          }
        }
      }
      else
      {
        int startIndex = activeTargetIndex >= 0 ? activeTargetIndex - 1 : targets.Count - 1;
        for (int i = startIndex; i >= 0; i--)
        {
          if (TryActivateTargetAndGoToOccurrence(targets[i], moveForward))
          {
            return true;
          }
        }
      }

      return false;
    }

    /// <summary>
    /// Переход к предыдущему вхождению.
    /// </summary>
    private void PreviousOccurrence(int searchArea, TextEditorUI textEditor)
    {
      if (foundResults.Count == 0)
      {
        MessageBoxCustom.Show($"Текст {_searchText} не найден", "Ошибка", image: MessageBoxImage.Error);
        return;
      }

      textEditor?.MarkerService?.ClearAllMarkers();

      if (currentIndex < 0)
      {
        currentIndex = foundResults.Count - 1;
        GoToOccurrence(currentIndex);
        return;
      }

      if (currentIndex > 0)
      {
        currentIndex--;
        GoToOccurrence(currentIndex);
        return;
      }

      if (searchArea == 1)
      {
        if (TrySwitchDocumentWithMatches(moveForward: false))
        {
          return;
        }

        MessageBoxCustom.Show("Достигнуто первое совпадение. Дополнительных вхождений в тексте нет.", "Поиск закончен", image: MessageBoxImage.Warning);
        return;
      }

      currentIndex = foundResults.Count - 1;
      GoToOccurrence(currentIndex);
    }

    /// <summary>
    /// Формирует последовательность всех вкладок редактора для навигации поиска.
    /// </summary>
    private List<(OpenFileButton page, TextEditorContainer container, DockItem item)> GetSearchNavigationTargets()
    {
      var targets = new List<(OpenFileButton page, TextEditorContainer container, DockItem item)>();
      var searchPages = SetSearchAreaPages(1);

      foreach (var searchPage in searchPages)
      {
        var dockItem = searchPage.Key;
        var page = searchPage.Value;
        int pageIndex = fileManager.EditorWorkspaceModel.OpenPages.IndexOf(page);

        if (pageIndex < 0 || pageIndex >= fileManager.EditorWorkspaceModel.UserControls.Count)
        {
          continue;
        }

        if (fileManager.EditorWorkspaceModel.UserControls[pageIndex] is TextEditorContainer textEditorContainer)
        {
          targets.Add((page, textEditorContainer, dockItem));
        }
      }

      return targets;
    }

    /// <summary>
    /// Находит индекс активной вкладки редактора в общей последовательности навигации.
    /// </summary>
    private int GetActiveSearchTargetIndex(List<(OpenFileButton page, TextEditorContainer container, DockItem item)> targets)
    {
      var activeTab = fileManager.EditorWorkspaceModel.OpenPages.FirstOrDefault(
        page => page.Background == (Brush)Application.Current.Resources["ActiveBorderSolidColorBrush"]);
      if (activeTab == null)
      {
        return -1;
      }

      int activePageIndex = fileManager.EditorWorkspaceModel.OpenPages.IndexOf(activeTab);
      if (activePageIndex < 0 || activePageIndex >= fileManager.EditorWorkspaceModel.UserControls.Count
        || fileManager.EditorWorkspaceModel.UserControls[activePageIndex] is not TextEditorContainer activeContainer)
      {
        return -1;
      }

      var activeDockItem = activeContainer.DockManager.DockItems.FirstOrDefault(item => item.IsActiveItem == true);
      if (activeDockItem == null)
      {
        return -1;
      }

      return targets.FindIndex(target => target.page == activeTab && target.item == activeDockItem);
    }

    /// <summary>
    /// Активирует вкладку документа, выполняет поиск в ней и переходит к нужному вхождению.
    /// </summary>
    private bool TryActivateTargetAndGoToOccurrence((OpenFileButton page, TextEditorContainer container, DockItem item) target, bool moveForward)
    {
      string pageText = GetDockItemText(target.item);
      if (string.IsNullOrEmpty(pageText))
      {
        return false;
      }

      var occurrences = FindAllOccurrences(pageText, _searchText, _wholeWord, _caseWord, _searchArea);
      if (occurrences == null || occurrences.Count == 0)
      {
        return false;
      }

      multiEditorControl.controlManager.ShowControl(target.container, target.page);
      target.item.Show(target.container.DockManager);

      currentIndex = moveForward ? 0 : foundResults.Count - 1;
      GoToOccurrence(currentIndex);
      return true;
    }

    /// <summary>
    /// Получает текст документа из вкладки редактора.
    /// </summary>
    private string GetDockItemText(DockItem dockItem)
    {
      if (dockItem?.Content is TextEditorUI textEditor)
      {
        return textEditor.Text;
      }

      if (dockItem?.Content is TranslatorItem translatorItem)
      {
        return translatorItem.GetLeftEditor()?.Text;
      }

      return null;
    }

    /// <summary>
    /// Переход к определенному вхождению.
    /// </summary>
    private void GoToOccurrence(int index)
    {
      if (index < 0 || index >= foundResults.Count)
      {
        return;
      }

      var activeTab = fileManager.EditorWorkspaceModel.OpenPages.FirstOrDefault(page => page.Background == (Brush)Application.Current.Resources["ActiveBorderSolidColorBrush"]);
      if (activeTab == null || fileManager.EditorWorkspaceModel.UserControls[fileManager.EditorWorkspaceModel.OpenPages.IndexOf(activeTab)] is not TextEditorContainer textEditorContainer)
      {
        return;
      }

      var activeDockItem = textEditorContainer.DockManager.DockItems.FirstOrDefault(item => item.IsActiveItem == true);
      if (activeDockItem == null)
      {
        return;
      }

      TextEditorUI textEditor;
      TranslatorItem translatorItem = null;

      if (activeDockItem.Content is TextEditorUI activeTextEditor)
      {
        textEditor = activeTextEditor;
      }
      else if (activeDockItem.Content is TranslatorItem activeTranslatorItem)
      {
        translatorItem = activeTranslatorItem;
        textEditor = activeTranslatorItem.GetLeftEditor();
      }
      else
      {
        return;
      }

      if (textEditor == null || textEditor.Document == null || textEditor.Document.TextLength == 0)
      {
        return;
      }

      var result = foundResults[index];
      if (result.StartOffset < 0 || result.StartOffset + result.Length > textEditor.Document.TextLength)
      {
        return;
      }

      textEditor.MarkerService.ClearAllMarkers();
      textEditor.MarkerService.AddMarker(result.StartOffset, result.Length, SearchHighlightColor);
      textEditor.TextArea.TextView.InvalidateLayer(KnownLayer.Selection);

      int lineNumber = textEditor.Document.GetLineByOffset(result.StartOffset).LineNumber;
      if (translatorItem != null)
      {
        HighlightTranslatorCommandBySourceLine(translatorItem, lineNumber);
      }

      textEditor.ScrollToLine(lineNumber);
      textEditor.TextArea.Caret.Offset = result.StartOffset + result.Length;
      textEditor.Focus();
      textEditor.TextArea.Focus();
      textEditor.TextArea.Caret.BringCaretToView();
    }

    private void HighlightTranslatorCommandBySourceLine(TranslatorItem translatorItem, int sourceLineNumber)
    {
      if (translatorItem == null)
      {
        return;
      }

      var leftEditor = translatorItem.GetLeftEditor();
      if (leftEditor == null)
      {
        return;
      }

      var rightEditor = translatorItem.GetRightEditor();
      if (rightEditor == null)
      {
        return;
      }

      rightEditor.MarkerService.ClearAllMarkers();

      var command = FindTranslatorCommandBySourceLine(translatorItem.TranslationModels, leftEditor, sourceLineNumber);
      if (command == null || command.FormattedStartLineNumber < 0)
      {
        return;
      }

      HighlightCommandHeader(rightEditor, command);
    }

    private static BaseCommandModel? FindTranslatorCommandBySourceLine(IEnumerable<BaseCommandModel>? commands, TextEditorUI leftEditor, int sourceLineNumber)
    {
      if (commands == null || leftEditor?.Document == null || sourceLineNumber <= 0)
      {
        return null;
      }

      var sourceHeader = FindNearestSourceCommandHeader(leftEditor, sourceLineNumber);
      if (sourceHeader.HasValue)
      {
        var (sourceCommandNumber, sourceMnemonic) = sourceHeader.Value;

        var exactCommand = commands.FirstOrDefault(model =>
          string.Equals(NormalizeCommandNumber(model.CommandNumber), sourceCommandNumber, StringComparison.Ordinal)
          && string.Equals(model.Mnemonic?.Trim(), sourceMnemonic, StringComparison.OrdinalIgnoreCase));

        if (exactCommand != null)
        {
          return exactCommand;
        }

        var numberMatchCommand = commands.FirstOrDefault(model =>
          string.Equals(NormalizeCommandNumber(model.CommandNumber), sourceCommandNumber, StringComparison.Ordinal));

        if (numberMatchCommand != null)
        {
          return numberMatchCommand;
        }
      }

      var orderedCommands = commands
        .Where(model => model != null && model.StartLineNumber > 0 && IsModelPresentInSource(leftEditor, model))
        .OrderBy(model => model.StartLineNumber)
        .ToList();

      if (orderedCommands.Count == 0)
      {
        orderedCommands = commands
          .Where(model => model != null && model.StartLineNumber > 0)
          .OrderBy(model => model.StartLineNumber)
          .ToList();

        if (orderedCommands.Count == 0)
        {
          return null;
        }
      }

      for (int i = 0; i < orderedCommands.Count; i++)
      {
        int startLine = orderedCommands[i].StartLineNumber;
        int nextStartLine = i < orderedCommands.Count - 1 ? orderedCommands[i + 1].StartLineNumber : int.MaxValue;

        if (sourceLineNumber >= startLine && sourceLineNumber < nextStartLine)
        {
          return orderedCommands[i];
        }
      }

      return orderedCommands.LastOrDefault(model => sourceLineNumber >= model.StartLineNumber);
    }

    private static (string CommandNumber, string Mnemonic)? FindNearestSourceCommandHeader(TextEditorUI leftEditor, int sourceLineNumber)
    {
      if (leftEditor?.Document == null || leftEditor.Document.LineCount == 0 || sourceLineNumber <= 0)
      {
        return null;
      }

      int lineToCheck = Math.Min(sourceLineNumber, leftEditor.Document.LineCount);
      for (int lineNumber = lineToCheck; lineNumber >= 1; lineNumber--)
      {
        var line = leftEditor.Document.GetLine(lineNumber);
        string lineText = leftEditor.Document.GetText(line);
        var match = Regex.Match(lineText, SourceCommandHeaderPattern);
        if (!match.Success)
        {
          continue;
        }

        string commandNumber = NormalizeCommandNumber(match.Groups["num"].Value);
        string mnemonic = match.Groups["mnemo"].Value.Trim();
        return (commandNumber, mnemonic);
      }

      return null;
    }

    private static bool IsModelPresentInSource(TextEditorUI leftEditor, BaseCommandModel model)
    {
      if (leftEditor?.Document == null || model == null)
      {
        return false;
      }

      int modelLine = model.StartLineNumber;
      if (modelLine <= 0 || modelLine > leftEditor.Document.LineCount)
      {
        return false;
      }

      var line = leftEditor.Document.GetLine(modelLine);
      string lineText = leftEditor.Document.GetText(line);
      var match = Regex.Match(lineText, SourceCommandHeaderPattern);
      if (!match.Success)
      {
        return false;
      }

      string lineCommandNumber = NormalizeCommandNumber(match.Groups["num"].Value);
      string modelCommandNumber = NormalizeCommandNumber(model.CommandNumber);
      if (!string.Equals(lineCommandNumber, modelCommandNumber, StringComparison.Ordinal))
      {
        return false;
      }

      if (string.IsNullOrWhiteSpace(model.Mnemonic))
      {
        return true;
      }

      return string.Equals(match.Groups["mnemo"].Value.Trim(), model.Mnemonic.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private void HighlightCommandHeader(TextEditorUI rightEditor, BaseCommandModel command)
    {
      if (rightEditor.Document == null)
      {
        return;
      }

      int formattedLineNumber = command.FormattedStartLineNumber + 1;
      if (formattedLineNumber < 1 || formattedLineNumber > rightEditor.Document.LineCount)
      {
        return;
      }

      var line = rightEditor.Document.GetLine(formattedLineNumber);
      string lineText = rightEditor.Document.GetText(line);
      if (string.IsNullOrWhiteSpace(lineText))
      {
        return;
      }

      string commandNumberPattern = BuildCommandNumberPattern(command.CommandNumber);
      string mnemonicPattern = string.IsNullOrWhiteSpace(command.Mnemonic)
        ? @"[А-ЯA-Z]{1,}"
        : Regex.Escape(command.Mnemonic);

      var strictHeaderMatch = Regex.Match(lineText, $@"^\s*(?<num>{commandNumberPattern})\s+(?<mnemo>{mnemonicPattern})\b");
      var headerMatch = strictHeaderMatch.Success
        ? strictHeaderMatch
        : Regex.Match(lineText, SourceCommandHeaderPattern);

      if (!headerMatch.Success)
      {
        return;
      }

      var commandNumberGroup = headerMatch.Groups["num"];
      if (commandNumberGroup.Success && commandNumberGroup.Length > 0)
      {
        rightEditor.MarkerService.AddMarker(line.Offset + commandNumberGroup.Index, commandNumberGroup.Length, SearchHighlightColor);
      }

      var mnemonicGroup = headerMatch.Groups["mnemo"];
      if (mnemonicGroup.Success && mnemonicGroup.Length > 0)
      {
        rightEditor.MarkerService.AddMarker(line.Offset + mnemonicGroup.Index, mnemonicGroup.Length, SearchHighlightColor);
      }

      rightEditor.TextArea.TextView.InvalidateLayer(KnownLayer.Selection);
      rightEditor.ScrollToLine(formattedLineNumber);
    }

    private static string BuildCommandNumberPattern(string commandNumber)
    {
      var normalizedCommandNumber = NormalizeCommandNumber(commandNumber);
      if (string.IsNullOrWhiteSpace(normalizedCommandNumber))
      {
        return @"\d+(?:\s+\d+)*";
      }

      var tokens = normalizedCommandNumber
        .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
        .Select(Regex.Escape)
        .ToArray();

      return tokens.Length > 0
        ? string.Join(@"\s+", tokens)
        : @"\d+(?:\s+\d+)*";
    }

    private static string NormalizeCommandNumber(string? commandNumber)
    {
      if (string.IsNullOrWhiteSpace(commandNumber))
      {
        return string.Empty;
      }

      return string.Join(" ", commandNumber
        .Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries));
    }

    /// <summary>
    /// Очистка подстветки.
    /// </summary>
    private void ClearHighlights(TextEditorUI textEditor, List<SearchResult> searchResults, ref int index)
    {
      textEditor?.MarkerService?.ClearAllMarkers();
      searchResults?.Clear();
      if (textEditor?.TextArea != null)
      {
        textEditor.TextArea.SelectionBorder = null;
      }
      index = -1;
    }

    private void ClearHighlights(TextEditorUI textEditor)
    {
      textEditor?.MarkerService?.ClearAllMarkers();
      if (textEditor?.TextArea != null)
      {
        textEditor.TextArea.SelectionBorder = null;
      }
    }

    /// <summary>
    /// Очистка подстветки.
    /// </summary>
    public void OnSearchWindowClosing()
    {
      var activeTab = fileManager.EditorWorkspaceModel.OpenPages.FirstOrDefault(page => page.Background == (Brush)Application.Current.Resources["ActiveBorderSolidColorBrush"]);
      if (activeTab != null && fileManager.EditorWorkspaceModel.UserControls[fileManager.EditorWorkspaceModel.OpenPages.IndexOf(activeTab)] is TextEditorContainer textEditorContainer)
      {
        if (textEditorContainer != null)
        {
          foreach (var control in textEditorContainer.DockManager.DockItems)
          {
            if (control.Content is TextEditorUI textEditor)
            {
              ClearHighlights(textEditor);
            }
            else if (control.Content is TranslatorItem item)
            {
              ClearHighlights(item.GetLeftEditor());
              ClearHighlights(item.GetRightEditor());
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
      var foundDockItem = new DockItem();
      if (foundPage.DockManager.DockItems.Any(item => item.Title == fileName))
      {
        foundDockItem = foundPage.DockManager.DockItems.FirstOrDefault(item => item.Title == fileName);
      }
      else
      {
        foreach (var tab in fileManager.EditorWorkspaceModel.OpenPages)
        {
          if (fileManager.EditorWorkspaceModel.UserControls[fileManager.EditorWorkspaceModel.OpenPages.IndexOf(tab)] is TextEditorContainer editorContainer)
          {
            foreach (var item in editorContainer.DockManager.DockItems)
            {
              if (item.Title == fileName)
              {
                foundDockItem = item;
                foundPage = editorContainer;
                multiEditorControl.controlManager.ShowControl(foundPage, tab);
              }
            }
          }
        }
      }
      TextEditorUI textEditor;
      TranslatorItem translatorItemForHighlight = null;
      if (foundDockItem.Content is TextEditorUI editor)
      {
        textEditor = editor;
      }
      else if (foundDockItem.Content is TranslatorItem translatorItem)
      {
        translatorItemForHighlight = translatorItem;
        textEditor = translatorItem.GetLeftEditor();
      }
      else
      {
        return;
      }

      if (textEditor != null)
      {

        if (textEditor.MarkerService == null)
        {
          LogError("markerService == null");
          return;
        }

        foundDockItem.Show(foundPage.DockManager);
        textEditor.MarkerService.ClearAllMarkers();

        var ranges = FindAllOccurrencesInLine(lineText, startOffset);

        textEditor.HighlightRanges(ranges);

        if (ranges.Count > 0)
        {
          textEditor.ScrollToLine(lineNumber);
        }

        if (translatorItemForHighlight != null)
        {
          HighlightTranslatorCommandBySourceLine(translatorItemForHighlight, lineNumber);
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
      var activeTab = fileManager.EditorWorkspaceModel.OpenPages.FirstOrDefault(page => page.Background == (Brush)Application.Current.Resources["ActiveBorderSolidColorBrush"]);
      if (activeTab != null && fileManager.EditorWorkspaceModel.UserControls[fileManager.EditorWorkspaceModel.OpenPages.IndexOf(activeTab)] is TextEditorContainer textEditorContainer)
      {
        return textEditorContainer;
      }
      else
      {
        return null;
      }
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
  }
}
