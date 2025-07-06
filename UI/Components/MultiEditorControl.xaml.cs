using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using UI.Components.Invoke;
using UI.Controls.TextEditor;
using Application = System.Windows.Application;
using UserControl = System.Windows.Controls.UserControl;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using UI.Components.SearchControls;
using UI.Components.MultiEditorMethods;
using AppConfiguration.Base;
using static UI.Components.Invoke.OpenFileButton;
using UI.Controls;
using UI.Windows.WpfDocking.Windows.Docking;

namespace UI.Components
{
  /// <summary>
  /// Логика взаимодействия для MultiEditorControl.xaml.
  /// </summary>
  public partial class MultiEditorControl : UserControl
  {
    /// <summary>
    /// Счетчик кликов для определения двойного клика.
    /// </summary>
    private int _clickCount = 0;

    /// <summary>
    /// Таймер для обработки двойного клика.
    /// </summary>
    private DispatcherTimer _clickTimer;

    /// <summary>
    /// Объект, управляющий операциями с файлами, включая открытие, сохранение и управление содержимым файлов.
    /// </summary>
    internal FileManager fileManager;

    /// <summary>
    /// Объект, управляющий операциями с поиском по тексту.
    /// </summary>
    internal TextSearchManager textSearchManager;

    /// <summary>
    /// Объект, управляющий операциями с поиском по тексту.
    /// </summary>
    internal TextReplacementManager textReplacementManager;

    /// <summary>
    /// Объект, управляющий операциями связанные с пользовательскими элементами управления.
    /// </summary>
    internal ControlManager controlManager;

    /// <summary>
    /// Объект, управляющей операциями связанными с сохранением файлов.
    /// </summary>
    internal SaveFileManager saveFileManager;

    /// <summary>
    /// Событие, которое вызывается, когда результаты поиска готовы для отображения.
    /// </summary>
    public event Action<string, bool?, Dictionary<string, List<SearchResult>>> SearchResultsReady;

    /// <summary>
    /// Инициализирует экземпляр <see cref="FileManager"/> и устанавливает связь с текущим контролом.
    /// </summary>
    public void InitializeManagers()
    {
      fileManager = new FileManager(this);
      textSearchManager = new TextSearchManager(fileManager, this);
      controlManager = new ControlManager(fileManager, this);
      saveFileManager = new SaveFileManager(fileManager);
      textReplacementManager = new TextReplacementManager(fileManager, this, controlManager);
    }

    /// <summary>
    /// Конструктор класса <see cref="MultiEditorControl"/>.
    /// Инициализирует компоненты и подписывается на необходимые события.
    /// </summary>
    public MultiEditorControl()
    {
      InitializeComponent();
      _clickTimer = new DispatcherTimer
      {
        Interval = TimeSpan.FromMilliseconds(300),
      };

      _clickTimer.Tick += (s, e) =>
      {
        _clickCount = 0;
        _clickTimer.Stop();
      };

      this.KeyDown += MultiWindowControl_KeyDown;
      EventAggregator.FoundTextSelectRow += OnFoundTextSelectRow;
      InitializeManagers();
    }

    private void OnFoundTextSelectRow(string fileName, int lineNumber, int startOffset, string lineText, string searchText)
    {
      textSearchManager.GetLineOccurrences(fileName, lineNumber, startOffset, lineText);
    }

    /// <summary>
    /// Обрабатывает событие нажатия левой кнопки мыши на верхней панели.
    /// При двойном клике создаёт новый файл.
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Данные события мыши.</param>
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

    /// <summary>
    /// Получает активный текстовый редактор.
    /// </summary>
    /// <returns>Если редатор найден возвращает экземпляр <see cref="TextEditorUI"/>, иначе возвраает null.</returns>
    public TextEditorUI GetActiveTextEditor()
    {
      return fileManager.GetActiveTextEditor();
    }

    public bool RemoveActiveTextEditor(bool isTranslation)
    {
      return fileManager.RemoveActiveTextEditor(isTranslation);
    }

    public void RemoveControl(EditorType editorType)
    {
      var control = fileManager.GetContainer(editorType);
      var page = fileManager.OpenPages.FirstOrDefault(item => item.Text == editorType.ToString());
      if (control != null && page != null)
      {
        controlManager.RemoveControl(page, control);
      }
    }

    /// <summary>
    /// Получает активный текстовый редактор.
    /// </summary>
    /// <returns>Если редатор найден возвращает экземпляр <see cref="TextEditorUI"/>, иначе возвраает null.</returns>
    public TextEditorContainer GetActiveTextEditorContainer(EditorType editorType)
    {
      return fileManager.GetContainer(editorType);
    }


    /// <summary>
    /// Получает активный текстовый редактор.
    /// </summary>
    /// <returns>
    /// Возвращает активный экземпляр <see cref="TextEditorUI"/>.
    /// </returns>
    public TextEditorUI CreateTranslationFileAsync()
    {
      return fileManager.CreateTranslationFileAsync();
    }

    /// <summary>
    /// Добавляет элемент управления и соответствующую вкладку в панель управления.
    /// </summary>
    /// <param name="header">Заголовок для кнопки, отображаемой в панели вкладок.</param>
    /// <param name="control">Элемент управления для отображения в панели управления.</param>
    /// <param name="description">Дополнительное описание для вкладки (опционально).</param>
    public void AddControl(string header, UserControl control, TypeWindow tabType, string description = null)
    {
      controlManager.AddControl(header, control, tabType, description);
    }

    /// <summary>
    /// Открывает диалоговое окно для открытия файла.
    /// </summary>
    /// <param name="path">Путь к файлу.</param>
    public void OpenFile(string path)
    {
      fileManager.OpenFile(path);
    }

    /// <summary>
    /// Создаёт новый файл.
    /// </summary>
    public void CreateNewFile()
    {
      fileManager.CreateNewFile();
    }

    /// <summary>
    /// Открывает диалоговое окно для сохранения файла в новом месте.
    /// В случае успешного сохранения, возвращает true, в противном случае false.
    /// </summary>
    /// <returns>True, если файл был успешно сохранен, иначе false.</returns>
    public bool SaveFileAs()
    {
      return saveFileManager.SaveFileAs();
    }

    /// <summary>
    /// Удаляет указанный элемент управления и соответствующую вкладку.
    /// </summary>
    /// <param name="tabButton">Вкладка для удаления.</param>
    /// <param name="control">Элемент управления для удаления.</param>
    private void RemoveControl(OpenFileButton tabButton, UserControl control)
    {
      controlManager.RemoveControl(tabButton, control);
    }

    /// <summary>
    /// Обрабатывает событие нажатия клавиш. 
    /// Позволяет закрыть активную вкладку при нажатии Alt+System+X.
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Данные события клавиатуры.</param>
    private void MultiWindowControl_KeyDown(object sender, KeyEventArgs e)
    {
      if (e.Key == Key.System && e.SystemKey == Key.X && Keyboard.Modifiers == ModifierKeys.Alt)
      {
        var activeTab = fileManager.OpenPages.FirstOrDefault(page =>
          page.Background == (Brush)Application.Current.Resources["ActiveBorderSolidColorBrush"]);
        if (activeTab != null)
        {
          int index = fileManager.OpenPages.IndexOf(activeTab);
          RemoveControl(activeTab, fileManager.UserControls[index]);
        }
      }
    }

    /// <summary>
    /// Обрабатывает событие закрытия окна поиска.
    /// </summary>
    public void OnSearchWindowClosing()
    {
      textSearchManager.OnSearchWindowClosing();
    }

    /// <summary>
    /// Обрабатывает сохранение файла.
    /// </summary>
    /// <returns>Результат сохранения файла. <c>true</c>, если файл был успешно сохранен, иначе <c>false</c>.</returns>
    public bool SaveFile()
    {
      var activeTab = fileManager.OpenPages.FirstOrDefault(page =>
        page.Background == (Brush)Application.Current.Resources["ActiveBorderSolidColorBrush"]);
      int index = fileManager.OpenPages.IndexOf(activeTab);
      if (fileManager.UserControls[index] is TextEditorContainer)
      {
        var activeTextEditorContainer = fileManager.UserControls[index] as TextEditorContainer;
        if (activeTextEditorContainer != null)
        {
          var activeDockItem = activeTextEditorContainer.DockManager.DockItems.FirstOrDefault(tab =>
            tab.IsActiveItem == true);
          return saveFileManager.SaveFile(activeDockItem);
        }
      }
      return false;
    }

    /// <summary>
    /// Выводит файл на печать.
    /// </summary>
    public void PrintFile()
    {
      PrintFileManager.PrintFile(fileManager.OpenPages, fileManager.UserControls);
    }

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
      textSearchManager.SearchResultsReady += OnSearchResultsReady;
      await textSearchManager.SearchData(searchText, wholeWord, caseWord, searchArea, searchParameters);
    }

    /// <summary>
    /// Выполняет поиск по тексту.
    /// </summary>
    /// <param name="replaceText">Текст, на который нужно заменить искомый текст.</param>
    /// <param name="searchText">Текст, который мы ищем.</param>
    /// <param name="wholeWord">Если true - ищем только слово целиком, false - ищем все вхождения заданного текста.</param>
    /// <param name="caseWord">Если true - учитываем регистр, false - не учитываем.</param>
    /// <param name="searchArea">Область поиска: поиск в текущем документе, во всех открытых документах, в файле.</param>
    /// <param name="searchParameters">Параметры поиска: найти  далее, найти предыдущее, найти все.</param>
    public async Task ReplaceWordData(string replaceText, string searchText, bool? wholeWord, bool? caseWord, int searchArea, string searchParameters)
    {
      var fullText = textSearchManager.GetText(searchArea);
      textSearchManager.InitializeSearch(fullText, searchText, wholeWord, caseWord, searchArea, searchParameters);
      await textSearchManager.FindAllAsync(searchText, wholeWord, caseWord, searchArea, false);
      if (string.Equals(searchParameters, "FindNext"))
      {
        ReplaceNextWord(replaceText, searchText);
      }
      else if (string.Equals(searchParameters, "FindAll"))
      {
        ReplaceAllWords(replaceText, searchText);
      }
    }

    private void ReplaceAllWords(string replaceText, string searchText)
    {
      if (textSearchManager.foundInOpenedFiles.Count > 0)
      {
        foreach (var file in textSearchManager.foundInOpenedFiles)
        {
          for (int i = file.Value.Count - 1; i >= 0; i--)
          {
            var result = file.Value[i];
            var lineStartOffset = result.StartOffset;
            int globalStartOffset = CalculateGlobalStartOffset(lineStartOffset, result.SubstringFromWord, searchText);
            if (globalStartOffset >= 0)
            {
              textReplacementManager.ReplaceWord(file.Key, result, globalStartOffset, replaceText, searchText);
            }
          }
        }
      }
    }

    private void ReplaceNextWord(string replaceText, string searchText)
    {
      if (textSearchManager.foundInOpenedFiles.Count > 0)
      {
        var searchResult = textSearchManager.foundInOpenedFiles.FirstOrDefault();
        var lineStartOffset = searchResult.Value.FirstOrDefault().StartOffset;
        if (lineStartOffset >= 0)
        {
          textReplacementManager.ReplaceWord(searchResult.Key, searchResult.Value.FirstOrDefault(), lineStartOffset, replaceText, searchText);
        }
      }
    }

    private int CalculateGlobalStartOffset(int lineStartOffset, string lineText, string searchText)
    {
      int wordStartIndex = lineText.IndexOf(searchText, StringComparison.Ordinal);

      if (wordStartIndex >= 0)
      {
        return lineStartOffset + wordStartIndex;
      }
      else
      {
        return -1;
      }
    }

    private void OnSearchResultsReady(string searchText, bool? isCaseSensitive, Dictionary<string, List<SearchResult>> results)
    {
      SearchResultsReady?.Invoke(searchText, isCaseSensitive, results);
    }

    internal Task<TranslatorItem> AddTranslatorItem(TextEditorUI editor, TextEditorUI translateEditor, EditorType editorType)
    {
      return fileManager.AddTranslatorItem(editor, translateEditor, editorType);
    }

    internal void OpenFolder()
    {
      fileManager.OpenFolder();
    }
  }
}
