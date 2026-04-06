using Ask.Core.Services.EventCore.Events;
using Ask.Core.Services.EventCore.Services;
using Ask.Core.Shared.Metadata.Static;
using Ask.Core.Shared.Metadata.View.EditorHost;
using Ask.Core.Shared.Metadata.View.EditorHost.TextEditor;
using Message;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using UI.Components.Invoke;
using UI.Components.SearchControls;
using UI.Controls;
using UI.Controls.TextEditorControl;
using static Ask.LogLib.LoggerUtility;
using UserControl = System.Windows.Controls.UserControl;

namespace UI.Components
{
  /// <summary>
  /// Логика взаимодействия для MultiWindowControl.xaml.
  /// </summary>
  public partial class MultiWindowControl : UserControl
  {
    /// <summary>
    /// Список вкладок открытых файлов. Каждая вкладка представлена экземпляром <see cref="OpenFileButton"/>.
    /// </summary>
    internal List<OpenFileButton> openPages = new List<OpenFileButton>();

    /// <summary>
    /// Список пользовательских элементов управления, соответствующих открытым вкладкам. Каждый элемент управления представляет собой экземпляр <see cref="UserControl"/>.
    /// </summary>
    internal List<UserControl> userControls = new List<UserControl>();

    public IRunService RunService => MultiEditor.RunService;

    public IEditorDocumentService EditorDocumentService => MultiEditor.EditorDocumentService;

    public IProtocolViewerService ProtocolViewerService => MultiEditor.ProtocolViewerService;
    public IWorkspaceService WorkspaceService => MultiEditor.WorkspaceService;

    public ITranslationService TranslationService => MultiEditor.TranslationService;


    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="MultiWindowControl"/>.
    /// </summary>
    /// <remarks>
    /// В конструкторе происходит инициализация компонента и подписка на событие закрытия текстового редактора <see cref="TextEditorClosing"/>.
    /// </remarks>
    public MultiWindowControl()
    {
      InitializeComponent();
      EventAggregator.Subscribe<EditorEvents.TextEditorContainerClosing>(e => OnTextEditorClosig(e.IsClosing, e.EditorName));
    }

    /// <summary>
    /// Обрабатывает события, происходящие при закрытии текстового редактора.
    /// </summary>
    /// <param name="textEditorClosing">Переменная, отвечающая за то закрывается текстовый редактор или вкладка другого типа.</param>
    /// <param name="textEditorName">Название файла, открытого в текстовом редакторе.</param>
    private void OnTextEditorClosig(bool textEditorClosing, string textEditorName)
    {
      if (textEditorClosing)
      {
        RemoveCorrespondingSearchDataGrid(textEditorName);
        CloseSearchResultsActions();
      }
    }

    /// <summary>
    /// Обрабатывает закрытие панеои с результатми поиска по тексту.
    /// </summary>
    private void CloseSearchResultsActions()
    {
      if (openPages.Count <= 0 && userControls.Count <= 0)
      {
        CloseSearchResults();
      }
    }

    /// <summary>
    /// Удаляет DataGrid с результатами поиска, соответствующий закрытому текстовому редактору.
    /// </summary>
    /// <param name="textEditorName">Имя закрываемого текстового редактора.</param>
    private void RemoveCorrespondingSearchDataGrid(string textEditorName)
    {
      var foundPage = openPages.FirstOrDefault(page => page.Text == textEditorName);
      if (foundPage != null)
      {
        int index = openPages.IndexOf(foundPage);
        if (userControls.Count > index && userControls[index] is SearchDataGrid activeDataGrid)
        {
          RemoveControl(foundPage, activeDataGrid);
        }
      }
    }

    /// <summary>
    /// Обрабатывает перемещение разделителя GridSplitter для изменения размера области результатов поиска.
    /// </summary>
    private void GridSplitter_DragDelta(object sender, DragDeltaEventArgs e)
    {
      if (SearchResultsRow == null)
      {
        return;
      }

      double totalHeight = ActualHeight;
      double editorsHeight = MultiEditor.ActualHeight;
      double minEditorsHeight = 100;
      double splitterHeight = MultiWindowSplitter.ActualHeight;
      SearchResultsRow.MinHeight = 35;

      double maxSearchResultsHeight = totalHeight - minEditorsHeight - splitterHeight;
      double newSearchHeight = totalHeight - editorsHeight - splitterHeight;

      if (newSearchHeight > maxSearchResultsHeight)
      {
        newSearchHeight = maxSearchResultsHeight;
      }

      SearchResultsRow.Height = new GridLength(newSearchHeight, GridUnitType.Pixel);
    }

    /// <summary>
    /// Скрывает или отображает панель результатов поиска при нажатии на кнопку "свернуть".
    /// </summary>
    private void minimizeButton_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      if (searchDataGrid.Visibility == Visibility.Visible)
      {
        SearchResultsRow.Height = new GridLength(35);
        searchDataGrid.Visibility = Visibility.Collapsed;
        MultiWindowSplitter.Visibility = Visibility.Collapsed;
      }
      else
      {
        SearchResultsRow.Height = new GridLength(200);
        searchDataGrid.Visibility = Visibility.Visible;
        MultiWindowSplitter.Visibility = Visibility.Visible;
      }
    }

    private void exitButton_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      CloseSearchResults();
    }

    private void CloseSearchResults()
    {
      SearchResultsRow.Height = new GridLength(0);
      SearchResultsRow.MinHeight = 0;
      SearchResults.Visibility = Visibility.Collapsed;
      MultiWindowSplitter.Visibility = Visibility.Collapsed;
    }

    /// <summary>
    /// Возвращает активный текстовый редактор указанного типа.
    /// </summary>
    /// <param name="editorType">Тип редактора (например, основной или транслятор).</param>
    /// <returns>
    /// Активный экземпляр <see cref="TextEditorUI"/>.
    /// Если редактор отсутствует — может вернуть <c>null</c>.
    /// </returns>
    public TextEditorUI GetActiveTextEditor(EditorType editorType)
    {
      return MultiEditor.GetActiveTextEditor(editorType);
    }

    /// <summary>
    /// Возвращает текущий активный пользовательский элемент в рабочей области.
    /// </summary>
    /// <returns>
    /// Экземпляр <see cref="UserControl"/>, отображаемый в данный момент,
    /// либо <c>null</c>, если активный элемент отсутствует.
    /// </returns>
    public UserControl? GetActiveWorkspaceControl()
    {
      return MultiEditor.ContentPanel.Children
        .OfType<UserControl>()
        .FirstOrDefault(control => control.Visibility == Visibility.Visible);
    }

    public UserControl? GetActiveWorkspaceControl()
    {
      return MultiEditor.ContentPanel.Children
        .OfType<UserControl>()
        .FirstOrDefault(control => control.Visibility == Visibility.Visible);
    }

    /// <summary>
    /// Возвращает активный текстовый редактор независимо от его типа.
    /// </summary>
    /// <returns>
    /// Активный экземпляр <see cref="TextEditorUI"/> или <c>null</c>, если редактор не найден.
    /// </returns>
    public TextEditorUI GetActiveTextEditor()
    {
      return MultiEditor.GetActiveTextEditor();
    }

    /// <param name="isTranslation">
    /// Указывает, выполняется ли закрытие в рамках операции трансляции.
    /// Может влиять на дополнительную обработку (например, очистку связанных вкладок).
    /// </param>
    /// <returns>
    /// <c>true</c>, если вкладка успешно закрыта;
    /// <c>false</c>, если закрытие не выполнено.
    /// </returns>
    public bool RemoveActiveTextEditor(bool isTranslation)
    {
      return MultiEditor.RemoveActiveTextEditor(isTranslation);
    }

    /// <summary>
    /// Пытается асинхронно закрыть текущую активную вкладку.
    /// </summary>
    /// <param name="eventAlreadyHandled">
    /// Указывает, было ли событие закрытия уже обработано ранее.
    /// Используется для предотвращения повторной обработки.
    /// </param>
    /// <returns>
    /// Задача, возвращающая <c>true</c>, если вкладка была закрыта;
    /// иначе <c>false</c>.
    /// </returns>
    public Task<bool> TryCloseActiveTabAsync(bool eventAlreadyHandled = false)
    {
      return MultiEditor.TryCloseActiveTabAsync(eventAlreadyHandled);
    }


    /// </summary>
    /// <param name="searchText">Искомый текст.</param>
    /// <param name="wholeWord">
    /// Если <c>true</c> — поиск выполняется только по целым словам;
    /// если <c>false</c> — по всем вхождениям.
    /// </param>
    /// <param name="caseWord">
    /// Если <c>true</c> — поиск чувствителен к регистру;
    /// если <c>false</c> — регистр игнорируется.
    /// </param>
    /// <param name="searchArea">
    /// Режим поиска (например: далее, назад, все вхождения).
    /// </param>
    /// <param name="searchParameters">
    /// Область поиска (текущий документ, все документы, файл и т.д.).
    /// </param>
    /// <remarks>
    /// В случае, если редактор не инициализирован, выводится сообщение об ошибке.
    /// </remarks>
    public async void SearchData(string searchText, bool? wholeWord, bool? caseWord, int searchArea, string searchParameters)
    {
      if (MultiEditor == null)
      {
        MessageBoxCustom.Show("Редактор не инициализирован!", "Ошибка", MessageBoxButton.OK, image: MessageBoxImage.Error);
        LogError("Редактор не инициализирован");

        return;
      }

      LogInformation($"Начат поиск по тексту. Искомый текст: {searchText}");
      await MultiEditor.SearchData(searchText, wholeWord, caseWord, searchArea, searchParameters);
    }

    /// <summary>
    /// Выполняет поиск по тексту в редакторе.
    /// </summary>
    /// <param name="searchText">
    /// Текст, который нужно найти.
    /// </param>
    /// <param name="wholeWord">
    /// Если true - ищем только слово целиком, иначе ищем все вхождения.
    /// </param>
    /// <param name="caseWord">
    /// Если true - учитываем регистр, иначе не учитываем.
    /// </param>
    /// <param name="searchArea">
    /// Параметры поиска: найти далее, найти предыдущее, найти все.
    /// </param>
    /// <param name="searchParameters">
    /// Область поиска: поиск в текущем документе, во всех открытых документах, в файле.
    /// </param>
    public async void ReplaceData(string replaceText, string searchText, bool? wholeWord, bool? caseWord, int searchArea, string searchParameters)
    {
      if (MultiEditor == null)
      {
        MessageBoxCustom.Show("Редактор не инициализирован!", "Ошибка", MessageBoxButton.OK, image: MessageBoxImage.Error);
        LogError("Редактор не инициализирован");

        return;
      }

      LogInformation($"Начат поиск по тексту. Искомый текст: {searchText}");
      CloseSearchResults();
      await MultiEditor.ReplaceWordData(replaceText, searchText, wholeWord, caseWord, searchArea, searchParameters);
    }

    /// <summary>
    /// Обрабатывает закрытие окна поиска.
    /// </summary>
    /// <remarks>
    /// Этот метод вызывает завершение обработки поиска и очистку ресурсов редактора, связанных с поиском.
    /// </remarks>
    public void OnSearchWindowClosing()
    {
      MultiEditor.OnSearchWindowClosing();
    }

    /// <summary>
    /// Отображает результаты поиска в области поиска.
    /// </summary>
    /// <param name="searchText">Текст, по которому был выполнен поиск.</param>
    /// <param name="isCaseSensitive">Учитывать ли регистр при поиске (true/false).</param>
    /// <param name="results">Результаты поиска для каждого файла.</param>
    public void ShowSearchResults(string searchText, bool? isCaseSensitive, Dictionary<string, List<SearchResult>> results)
    {
      int totalCount = 0;
      PrepareSearchResultsArea();

      foreach (var file in results)
      {
        List<SearchResult> items = CreateSearchResultItems(file, searchText, isCaseSensitive);

        totalCount += items.Count;

        LogSearchResults(file.Key, searchText, items.Count);

        var searchResultsForFile = new SearchDataGrid();
        AddControlInSearchArea(file.Key, searchResultsForFile);
        searchResultsForFile.SetItemSourse(items);
      }

      ShowSearchResultsArea();
      DisplayOverallSearchResults(searchText, totalCount);
    }

    /// <summary>
    /// Отображает область для отображения результатов поиска.
    /// </summary>
    private void ShowSearchResultsArea()
    {
      MultiWindowSplitter.Visibility = Visibility.Visible;
      SearchResultsRow.Height = new GridLength(200);
      SearchResults.Visibility = Visibility.Visible;
    }

    /// <summary>
    /// Создает список объектов SearchResult для каждого вхождения в файле.
    /// </summary>
    /// <param name="file">Результаты поиска для одного файла.</param>
    /// <param name="searchText">Текст, по которому был выполнен поиск.</param>
    /// <param name="isCaseSensitive">Учитывать ли регистр при поиске.</param>
    /// <returns>Список найденных результатов.</returns>
    private List<SearchResult> CreateSearchResultItems(KeyValuePair<string, List<SearchResult>> file, string searchText, bool? isCaseSensitive)
    {
      var caseValue = isCaseSensitive ?? false; // Если флаг null, считаем, что поиск не чувствителен к регистру
      List<SearchResult> items = new List<SearchResult>();
      var searchResultsForFile = new SearchDataGrid();

      foreach (var occurrence in file.Value)
      {
        items.Add(new SearchResult(
            occurrence.StartOffset,
            occurrence.Length,
            occurrence.LineNumber,
            occurrence.SubstringFromWord,
            file.Key,
            searchText,
            caseValue));
      }

      return items;
    }

    /// <summary>
    /// Логирует информацию о найденных результатах в файле.
    /// </summary>
    /// <param name="fileName">Имя файла, в котором были найдены результаты.</param>
    /// <param name="searchText">Текст, по которому был выполнен поиск.</param>
    /// <param name="count">Количество найденных строк.</param>
    private void LogSearchResults(string fileName, string searchText, int count)
    {
      var searchResultsText = $"Результаты поиска по \"{searchText}\" в файле \"{fileName}\". Найдено {count} строк";
      LogInformation(searchResultsText);
    }

    /// <summary>
    /// Отображает общие результаты поиска для всех файлов.
    /// </summary>
    /// <param name="searchText">Текст, по которому был выполнен поиск.</param>
    /// <param name="totalCount">Общее количество найденных строк.</param>
    private void DisplayOverallSearchResults(string searchText, int totalCount)
    {
      string overallSearchText = $"Результаты поиска по \"{searchText}\". Всего найдено {totalCount} строк";
      searchResultsTextBlock.Text = overallSearchText;
    }

    /// <summary>
    /// Подготавливает область вывода результатов поиска по тексту.
    /// </summary>
    private void PrepareSearchResultsArea()
    {
      SearchResultsTopPanel.Children.Clear();
      ContentPanel.Children.Clear();
      openPages.Clear();
      userControls.Clear();
      searchResultsTextBlock.Text = string.Empty;
    }

    /// <summary>
    /// Метод, который вызывается после применения шаблона для элемента.
    /// Подписывается на событие <see cref="MultiEditor.SearchResultsReady"/> для отображения результатов поиска.
    /// </summary>
    public override void OnApplyTemplate()
    {
      base.OnApplyTemplate();

      if (MultiEditor != null)
      {
        MultiEditor.SearchResultsReady += ShowSearchResults;
      }
    }

    /// <summary>
    /// Добавляет новую вкладку с пользовательским элементом управления в область результатов поиска.
    /// </summary>
    /// <param name="header">Заголовок вкладки.</param>
    /// <param name="control">Элемент управления для отображения содержимого.</param>
    /// <param name="description">
    /// Дополнительное описание вкладки (используется для идентификации и предотвращения дубликатов).
    /// </param>
    /// <remarks>
    /// Если вкладка с таким описанием или заголовком уже существует — она будет активирована,
    /// а не создана заново.
    /// </remarks>
    public void AddControlInSearchArea(string header, UserControl control, string description = null)
    {
      OpenFileButton tabButton = CreateTabButton(header, description);

      if (TryShowExistingControl(description, header))
      {
        return;
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

      AddControlToCollections(tabButton, control);

      try
      {
        AddControlToUI(control, tabButton);
      }
      finally
      {
        ShowControl(control, tabButton);
      }
    }

    /// <summary>
    /// Создает кнопку вкладки с заданным заголовком и описанием.
    /// </summary>
    /// <param name="header">Заголовок для кнопки.</param>
    /// <param name="description">Описание вкладки (если оно есть).</param>
    /// <returns>Созданная кнопка вкладки.</returns>
    private OpenFileButton CreateTabButton(string header, string description)
    {
      OpenFileButton tabButton = new OpenFileButton();
      tabButton.Header.Text = header;

      if (!string.IsNullOrEmpty(description))
      {
        tabButton.Description = description;
      }

      return tabButton;
    }

    /// <summary>
    /// Пытается показать существующую вкладку, если она уже есть в коллекции.
    /// </summary>
    /// <param name="description">Описание вкладки для поиска.</param>
    /// <param name="header">Заголовок вкладки для поиска.</param>
    /// <returns>True, если вкладка была найдена и показана; иначе False.</returns>
    private bool TryShowExistingControl(string description, string header)
    {
      foreach (OpenFileButton page in openPages)
      {
        if ((description != null && page.Description == description) || page.Header.Text == header)
        {
          int index = openPages.IndexOf(page);
          var userControl = userControls[index];
          ShowControl(userControl, page);

          return true;
        }
      }

      return false;
    }

    /// <summary>
    /// Добавляет вкладку и элемент управления в соответствующие коллекции.
    /// </summary>
    /// <param name="tabButton">Кнопка вкладки.</param>
    /// <param name="control">Элемент управления.</param>
    private void AddControlToCollections(OpenFileButton tabButton, UserControl control)
    {
      openPages.Add(tabButton);
      userControls.Add(control);
    }

    /// <summary>
    /// Добавляет элемент управления и вкладку в UI.
    /// </summary>
    /// <param name="control">Элемент управления.</param>
    /// <param name="tabButton">Кнопка вкладки.</param>
    private void AddControlToUI(UserControl control, OpenFileButton tabButton)
    {
      ContentPanel.Children.Add(control);
      SearchResultsTopPanel.Children.Add(tabButton);
    }

    /// <summary>
    /// Отображает указанный элемент управления, скрывая остальные.
    /// </summary>
    /// <param name="control">Элемент управления для отображения.</param>
    private void ActivePage(OpenFileButton control)
    {
      foreach (OpenFileButton child in SearchResultsTopPanel.Children)
      {
        child.IsActive = control == child;
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
        int index = ContentPanel.Children.IndexOf(control);

        if (index > 0)
        {
          index--;
        }

        openPages.Remove(tabButton);
        userControls.Remove(control);

        SearchResultsTopPanel.Children.Remove(tabButton);
        ContentPanel.Children.Remove(control);

        if (ContentPanel.Children.Count > 0)
        {
          ShowControl(userControls[index], openPages[index]);
        }

        CloseSearchResultsActions();
      }
    }

    /// <summary>
    /// Возвращает активный контейнер вкладок редактора указанного типа.
    /// </summary>
    /// <param name="editorType">Тип редактора.</param>
    /// <returns>
    /// Экземпляр <see cref="TextEditorContainer"/> для активной области.
    /// </returns>
    public TextEditorContainer GetActiveTextEditorContainer(EditorType editorType)
    {
      return MultiEditor.GetActiveTextEditorContainer(editorType);
    }

    /// <summary>
    /// Добавляет вкладку транслятора (связанную пару редакторов: исходный и результат).
    /// </summary>
    /// <param name="editor">Редактор исходного файла.</param>
    /// <param name="translateEditor">Редактор результата трансляции.</param>
    /// <param name="editorType">Тип вкладки.</param>
    /// <returns>
    /// Задача, возвращающая созданный <see cref="TranslatorItem"/>.
    /// </returns>
    public Task<TranslatorItem> AddTranslatorItem(ITextEditorView editor, ITextEditorView translateEditor, EditorType editorType)
    {
      return MultiEditor.AddTranslatorItem(editor, translateEditor, editorType);
    }

    /// <summary>
    /// Удаляет вкладку транслятора.
    /// </summary>
    /// <param name="translatorItem">Удаляемый элемент транслятора.</param>
    /// <param name="editorType">Тип вкладки.</param>
    /// <returns>Асинхронная операция удаления.</returns>
    public async Task DeleteTranslatorItem(TranslatorItem translatorItem, EditorType editorType)
    {
      await MultiEditor.DeleteTranslatorItem(translatorItem, editorType);
    }
  }
}
