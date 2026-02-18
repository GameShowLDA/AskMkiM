using Ask.Core.Services.EventCore.Events;
using Ask.Core.Services.EventCore.Services;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Metadata.Enums.UiEnums;
using Ask.Core.Shared.Metadata.Static;
using Ask.Core.Shared.Metadata.View.EditorHost;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using UI.Components.Invoke;
using UI.Components.MultiEditorMethods;
using UI.Components.SearchControls;
using UI.Controls;
using UI.Controls.ProtocolNew;
using UI.Controls.Runner;
using UI.Controls.TextEditor;
using static UI.Components.Invoke.OpenFileButton;
using Application = System.Windows.Application;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using UserControl = System.Windows.Controls.UserControl;

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

    public IRunService RunService => fileManager.RunControlService;

    public IEditorDocumentService EditorDocumentService => fileManager.EditorDocumentService;

    public IProtocolViewerService ProtocolViewerService => fileManager.ProtocolService;

    public IWorkspaceService WorkspaceService => controlManager;


    /// <summary>
    /// Событие, которое вызывается, когда результаты поиска готовы для отображения.
    /// </summary>
    public event Action<string, bool?, Dictionary<string, List<SearchResult>>> SearchResultsReady;

    private static readonly DependencyPropertyKey CountsPropertyKey =
    DependencyProperty.RegisterReadOnly(
      nameof(Counts),
      typeof(int),
      typeof(MultiEditorControl),
      new FrameworkPropertyMetadata(0));

    public static readonly DependencyProperty CountsProperty = CountsPropertyKey.DependencyProperty;

    /// <summary>Текущее число контролов внутри редактора.</summary>
    public int Counts
    {
      get => (int)GetValue(CountsProperty);
      private set => SetValue(CountsPropertyKey, value);
    }

    /// <summary>
    /// Инициализирует экземпляр <see cref="FileManager"/> и устанавливает связь с текущим контролом.
    /// </summary>
    public void InitializeManagers()
    {
      fileManager = new FileManager(this);
      textSearchManager = new TextSearchManager(fileManager, this);
      controlManager = new ControlManager(fileManager, this);
      textReplacementManager = new TextReplacementManager(fileManager);
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

      this.RemoveHandler(Keyboard.PreviewKeyDownEvent, new KeyEventHandler(MultiWindowControl_KeyDown));
      this.AddHandler(Keyboard.PreviewKeyDownEvent, new KeyEventHandler(MultiWindowControl_KeyDown), true);

      EventAggregator.Subscribe<SearchEvents.FoundTextSelectRow>(e => OnFoundTextSelectRow(e.FileName, e.LineNumber, e.StartOffset, e.LineText, e.SearchText));

      ProtocolUI.AnotherKeyPressed -= MultiWindowControl_KeyDown;
      ProtocolUI.AnotherKeyPressed += MultiWindowControl_KeyDown;
      InitializeManagers();

      Counts = fileManager.EditorWorkspaceModel.UserControls.Count;
      fileManager.EditorWorkspaceModel.UserControls.CollectionChanged += (s, a) =>
      {
        Counts = fileManager.EditorWorkspaceModel.UserControls.Count;
      };
    }

    #region События.
    private void OnFoundTextSelectRow(string fileName, int lineNumber, int startOffset, string lineText, string searchText) =>
      textSearchManager.GetLineOccurrences(fileName, lineNumber, startOffset, lineText);

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
        EditorDocumentService.CreateNewFile();
      }
    }

    /// <summary>
    /// Обрабатывает событие нажатия клавиш.
    /// Позволяет закрыть активную вкладку при нажатии Ctrl+F4.
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Данные события клавиатуры.</param>
    private async void MultiWindowControl_KeyDown(object sender, KeyEventArgs e)
    {
      if (e.Handled || !IsCloseActiveTabShortcut(e))
      {
        return;
      }

      if (await TryCloseActiveTabAsync(e.Handled))
      {
        e.Handled = true;
      }
    }

    internal async Task<bool> TryCloseActiveTabAsync(bool eventAlreadyHandled)
    {
      if (!TryGetActiveTab(out var activeTab, out int index))
      {
        return false;
      }

      if (fileManager.EditorWorkspaceModel.UserControls[index] is TextEditorContainer textEditorContainer)
      {
        // Для текстового контейнера Ctrl+F4 также обрабатывается самим DockManager.
        // Если событие уже обработано, повторно не закрываем, чтобы избежать двойного закрытия.
        if (eventAlreadyHandled)
        {
          return false;
        }

        var foundItem = textEditorContainer.DockManager.DockItems.FirstOrDefault(item => item.IsActiveDocument == true);
        if (foundItem == null)
        {
          return false;
        }

        foundItem.PerformClose();

        return true;
      }

      await controlManager.RemoveControl(activeTab, fileManager.EditorWorkspaceModel.UserControls[index]);

      return true;
    }

    private static bool IsCloseActiveTabShortcut(KeyEventArgs e)
    {
      bool isCtrlF4 = (e.Key == Key.F4 || (e.Key == Key.System && e.SystemKey == Key.F4))
        && Keyboard.Modifiers == ModifierKeys.Control;

      return isCtrlF4;
    }

    private bool TryGetActiveTab(out OpenFileButton activeTab, out int index)
    {
      activeTab = fileManager.EditorWorkspaceModel.OpenPages.FirstOrDefault(page =>
        page.Background == (Brush)Application.Current.Resources["ActiveBorderSolidColorBrush"]);
      index = activeTab == null ? -1 : fileManager.EditorWorkspaceModel.OpenPages.IndexOf(activeTab);

      if (activeTab == null || index < 0 || index >= fileManager.EditorWorkspaceModel.UserControls.Count)
      {
        var visibleControl = fileManager.EditorWorkspaceModel.UserControls
          .FirstOrDefault(control => control.Visibility == Visibility.Visible);
        if (visibleControl != null)
        {
          index = fileManager.EditorWorkspaceModel.UserControls.IndexOf(visibleControl);
          if (index >= 0 && index < fileManager.EditorWorkspaceModel.OpenPages.Count)
          {
            activeTab = fileManager.EditorWorkspaceModel.OpenPages[index];
          }
        }
      }

      if (activeTab == null)
      {
        return false;
      }

      if (index < 0 || index >= fileManager.EditorWorkspaceModel.UserControls.Count)
      {
        return false;
      }

      return true;
    }

    /// <summary>
    /// Обрабатывает событие закрытия окна поиска.
    /// </summary>
    public void OnSearchWindowClosing() => textSearchManager.OnSearchWindowClosing();

    private void OnSearchResultsReady(string searchText, bool? isCaseSensitive, Dictionary<string, List<SearchResult>> results) =>
     SearchResultsReady?.Invoke(searchText, isCaseSensitive, results);

    #endregion

    #region TextEditor

    /// <summary>
    /// Получает активный текстовый редактор.
    /// </summary>
    /// <returns>Если редатор найден возвращает экземпляр <see cref="TextEditorUI"/>, иначе возвраает null.</returns>
    public TextEditorUI GetActiveTextEditor(EditorType editorType) => fileManager.TextEditorService.GetActiveTextEditor(editorType);

    /// <summary>
    /// Получает активный текстовый редактор.
    /// </summary>
    /// <returns>Если редатор найден возвращает экземпляр <see cref="TextEditorUI"/>, иначе возвраает null.</returns>
    public TextEditorUI GetActiveTextEditor() => fileManager.TextEditorService.GetActiveTextEditor();

    /// <summary>
    /// Закрывает вкладку с активным текстовым редактором.
    /// </summary>
    /// <param name="isTranslation">Переменная, показывающая, выполняется закрытие вкладки при трансляции или нет.</param>
    /// <returns>Возвращает <c>true</c>, если вкладка была закрыта, <c>false</c> в противном случае.</returns>
    public bool RemoveActiveTextEditor(bool isTranslation) => fileManager.TextEditorService.CloseActiveTextEditor(isTranslation);

    #endregion

    #region Container

    /// <summary>
    /// Получает активный текстовый редактор.
    /// </summary>
    /// <returns>Если редатор найден возвращает экземпляр <see cref="TextEditorUI"/>, иначе возвраает null.</returns>
    public TextEditorContainer GetActiveTextEditorContainer(EditorType editorType) => fileManager.ContainerService.GetEditorContainer(editorType);

    #endregion

    #region Translator

    /// <summary>
    /// Получает активный текстовый редактор.
    /// </summary>
    /// <returns>
    /// Возвращает активный экземпляр <see cref="TextEditorUI"/>.
    /// </returns>
    public TextEditorUI CreateTranslationFileAsync(string parentFilePath) => fileManager.TranslationService.CreateTranslationEditor(parentFilePath);

    #endregion


    public bool GetEmtyControl() => controlManager.GetEmtyControl();


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
        bool replaced = ReplaceNextWord(replaceText, searchText);
        if (replaced)
        {
          await textSearchManager.SearchData(searchText, wholeWord, caseWord, searchArea, "FindNext");
        }
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
            textReplacementManager.ReplaceWord(file.Key, result, result.StartOffset, replaceText, searchText);
          }
        }
      }
    }

    private bool ReplaceNextWord(string replaceText, string searchText)
    {
      if (textSearchManager.foundInOpenedFiles.Count > 0)
      {
        var activeTab = fileManager.EditorWorkspaceModel.OpenPages.FirstOrDefault(
          page => page.Background == (Brush)Application.Current.Resources["ActiveBorderSolidColorBrush"]);

        if (activeTab != null)
        {
          int pageIndex = fileManager.EditorWorkspaceModel.OpenPages.IndexOf(activeTab);
          if (pageIndex >= 0 && pageIndex < fileManager.EditorWorkspaceModel.UserControls.Count
            && fileManager.EditorWorkspaceModel.UserControls[pageIndex] is TextEditorContainer textEditorContainer)
          {
            var activeDockItem = textEditorContainer.DockManager.DockItems.FirstOrDefault(item => item.IsActiveItem == true);
            TextEditorUI textEditor = null;
            string activeTitle = null;

            if (activeDockItem != null)
            {
              activeTitle = activeDockItem.Title;
              if (activeDockItem.Content is TextEditorUI te)
              {
                textEditor = te;
              }
              else if (activeDockItem.Content is TranslatorItem translatorItem)
              {
                textEditor = translatorItem.GetLeftEditor();
              }
            }

            if (!string.IsNullOrEmpty(activeTitle) && textSearchManager.foundInOpenedFiles.TryGetValue(activeTitle, out var occurrences) && occurrences.Count > 0)
            {
              var selectedOccurrence = TryGetSelectedOccurrence(textEditor, occurrences, searchText);
              int caretOffset = textEditor?.TextArea?.Caret?.Offset ?? 0;
              var currentOccurrence = TryGetCurrentOccurrenceByCaret(occurrences, caretOffset);
              var next = selectedOccurrence
                ?? currentOccurrence
                ?? occurrences.FirstOrDefault(r => r.StartOffset >= caretOffset)
                ?? occurrences.First();

              if (next != null)
              {
                textReplacementManager.ReplaceWord(activeTitle, next, next.StartOffset, replaceText, searchText);
                return true;
              }
            }
          }
        }

        // Fallback: используем первое совпадение, если активный документ не определён
        var first = textSearchManager.foundInOpenedFiles.FirstOrDefault();
        var firstOccurrence = first.Value.FirstOrDefault();
        if (firstOccurrence != null)
        {
          textReplacementManager.ReplaceWord(first.Key, firstOccurrence, firstOccurrence.StartOffset, replaceText, searchText);
          return true;
        }
      }

      return false;
    }

    private static SearchResult TryGetCurrentOccurrenceByCaret(List<SearchResult> occurrences, int caretOffset)
    {
      if (occurrences == null || occurrences.Count == 0)
      {
        return null;
      }

      return occurrences.FirstOrDefault(occurrence =>
        occurrence.StartOffset <= caretOffset && caretOffset <= occurrence.StartOffset + occurrence.Length);
    }

    private SearchResult TryGetSelectedOccurrence(TextEditorUI textEditor, List<SearchResult> occurrences, string searchText)
    {
      if (textEditor?.TextEditor == null || occurrences == null || occurrences.Count == 0)
      {
        return null;
      }

      if (textEditor.TextEditor.SelectionLength <= 0)
      {
        return null;
      }

      if (!IsMatchedSelectedText(textEditor.TextEditor.SelectedText, searchText, textSearchManager._caseWord))
      {
        return null;
      }

      int selectionStart = textEditor.TextEditor.SelectionStart;
      int selectionLength = textEditor.TextEditor.SelectionLength;
      return occurrences.FirstOrDefault(occurrence => occurrence.StartOffset == selectionStart && occurrence.Length == selectionLength);
    }

    private static bool IsMatchedSelectedText(string selectedText, string searchText, bool? caseWord)
    {
      if (string.IsNullOrEmpty(selectedText) || string.IsNullOrEmpty(searchText))
      {
        return false;
      }

      var comparison = caseWord == true ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
      return string.Equals(selectedText, searchText, comparison);
    }

    internal Task<TranslatorItem> AddTranslatorItem(TextEditorUI editor, TextEditorUI translateEditor, EditorType editorType) =>
      fileManager.TranslationService.AddTranslatorItem(editor, translateEditor, editorType);


    internal async Task DeleteTranslatorItem(TranslatorItem translatorItem, EditorType editorType) =>
      await fileManager.TranslationService.RemoveTranslatorTabAsync(translatorItem, editorType);
  }
}
