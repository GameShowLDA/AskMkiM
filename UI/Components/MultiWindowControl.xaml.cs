using AppConfiguration.Base;
using DevZest.Windows.Docking;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using UI.Components.Invoke;
using UI.Components.MultiEditorMethods;
using UI.Components.SearchControls;
using UI.Controls;
using UI.Controls.TextEditor;
using static UI.Components.Invoke.OpenFileButton;
using static Utilities.LoggerUtility;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;
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

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="MultiWindowControl"/>.
    /// </summary>
    /// <remarks>
    /// В конструкторе происходит инициализация компонента и подписка на событие закрытия текстового редактора <see cref="TextEditorClosing"/>.
    /// </remarks>
    public MultiWindowControl()
    {
      InitializeComponent();
      EventAggregator.TextEditorContainerClosing += OnTextEditorClosig;
    }

    private void OnTextEditorClosig(bool textEditorClosing, string textEditorName)
    {
      if (textEditorClosing)
      {
        RemoveCorrespondingSearchDataGrid(textEditorName);
        CloseSearchResultsActions();
      }
    }

    private void CloseSearchResultsActions()
    {
      if (openPages.Count <= 0 && userControls.Count <= 0)
      {
        CloseSearchResults();
        EventAggregator.RaiseCloseSearchWindow();
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
    /// Добавляет новый MultiEditorControl в контейнер.
    /// </summary>
    /// <param name="filePath">Путь к файлу.</param>
    public void OpenFileInEditor(string filePath)
    {
      if (MultiEditor == null)
      {
        MessageBox.Show("Редактор не инициализирован!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        LogError("Редактор не инициализирован");
        return;
      }

      MultiEditor.OpenFile(filePath);
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
    /// Получает активный текстовый редактор.
    /// </summary>
    /// <returns>
    /// Возвращает активный экземпляр <see cref="TextEditorUI"/>.
    /// </returns>
    public TextEditorUI GetActiveTextEditor()
    {
      return MultiEditor.GetActiveTextEditor();
    }

    public bool RemoveActiveTextEditor()
    {
      return MultiEditor.RemoveActiveTextEditor();
    }

    public void RemoveControl(EditorType editorType)
    {
      MultiEditor.RemoveControl(editorType);
    }

    /// <summary>
    /// Получает активный текстовый редактор.
    /// </summary>
    /// <returns>
    /// Возвращает активный экземпляр <see cref="TextEditorUI"/>.
    /// </returns>
    public TextEditorUI CreateTranslationFileAsync()
    {
      return MultiEditor.CreateTranslationFileAsync();
    }

    /// <summary>
    /// Добавляет новый элемент управления в редактор.
    /// </summary>
    /// <param name="name">
    /// Имя для нового элемента управления.
    /// </param>
    /// <param name="userControl">
    /// Элемент управления, который будет добавлен.
    /// </param>
    public void AddControl(string name, UserControl userControl, TypeWindow tabType)
    {
      MultiEditor.AddControl(name, userControl, tabType);
    }

    /// <summary>
    /// Создает новый файл в редакторе.
    /// </summary>
    /// <remarks>
    /// Этот метод вызывает создание нового файла в редакторе, если редактор был инициализирован.
    /// Если редактор не инициализирован, выводится сообщение об ошибке.
    /// </remarks>
    public void CreateNewFile()
    {
      if (MultiEditor == null)
      {
        MessageBox.Show("Редактор не инициализирован!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        LogError("Редактор не инициализирован");
        return;
      }

      MultiEditor.CreateNewFile();
    }

    /// <summary>
    /// Сохраняет текущий файл в редакторе.
    /// </summary>
    /// <remarks>
    /// Этот метод вызывает сохранение файла в редакторе. Если редактор не инициализирован,
    /// выводится сообщение об ошибке.
    /// </remarks>
    public void SaveFile()
    {
      if (MultiEditor == null)
      {
        MessageBox.Show("Редактор не инициализирован!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        LogError("Редактор не инициализирован");
        return;
      }

      MultiEditor.SaveFile();
    }

    /// <summary>
    /// Сохраняет файл под новым именем.
    /// </summary>
    /// <remarks>
    /// Этот метод вызывает сохранение файла под новым именем в редакторе. Если редактор не инициализирован,
    /// выводится сообщение об ошибке.
    /// </remarks>
    public void SaveFileAs()
    {
      if (MultiEditor == null)
      {
        MessageBox.Show("Редактор не инициализирован!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        LogError("Редактор не инициализирован");
        return;
      }

      MultiEditor.SaveFileAs();
    }

    /// <summary>
    /// Отправляет текущий файл на печать.
    /// </summary>
    /// <remarks>
    /// Этот метод вызывает функцию печати файла в редакторе. Если редактор не инициализирован,
    /// выводится сообщение об ошибке.
    /// </remarks>
    public void PrintFile()
    {
      if (MultiEditor == null)
      {
        MessageBox.Show("Редактор не инициализирован!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        LogError("Редактор не инициализирован");
        return;
      }

      MultiEditor.PrintFile();
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
    public async void SearchData(string searchText, bool? wholeWord, bool? caseWord, int searchArea, string searchParameters)
    {
      if (MultiEditor == null)
      {
        MessageBox.Show("Редактор не инициализирован!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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
        MessageBox.Show("Редактор не инициализирован!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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
    /// Добавляет элемент управления и кнопку в соответствующие панели поиска.
    /// </summary>
    /// <param name="header">Заголовок для кнопки.</param>
    /// <param name="control">Элемент управления для отображения.</param>
    /// <param name="description">Описание для вкладки, если оно есть.</param>
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

    public TextEditorContainer GetActiveTextEditorContainer(EditorType editorType)
    {
      return MultiEditor.GetActiveTextEditorContainer(editorType);
    }

    public void AddTranslatorItem(TextEditorUI editor, TextEditorUI translateEditor, EditorType editorType)
    {
      MultiEditor.AddTranslatorItem(editor, translateEditor, editorType);
    }
  }
}
