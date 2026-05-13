using Ask.Core.Services.EventCore.Events;
using Ask.Core.Services.EventCore.Services;
using Ask.UI.Features.Archive.Views;
using ICSharpCode.AvalonEdit;
using MainWindowProgram.HotkeyBindings;
using MainWindowProgram.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using UI.Components;
using UI.Controls.TextEditorControl;

namespace MainWindowProgram.Events
{
  /// <summary>
  /// Класс <c>UiEventsBinder</c> подписывает обработчики событий пользовательского интерфейса (UI),
  /// и управляет реакцией главного окна на эти события.
  /// </summary>
  public class UiEventsBinder
  {
    /// <summary>
    /// Ссылка на главное окно приложения, используемая для управления его элементами.
    /// </summary>
    private readonly MainWindow _mainWindow;

    /// <summary>
    /// Контейнер для управления несколькими редакторами внутри интерфейса.
    /// </summary>
    private readonly MultiWindowControl _multiWindow;

    private readonly TextEditorStatusViewModel _statusBarViewModel;

    private TextEditor? _lastEditorControl;

    /// <summary>
    /// Свойство, указывающее, открыто ли окно поиска.
    /// </summary>
    public bool IsSearchWindowOpen { get; set; }

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="UiEventsBinder"/>.
    /// </summary>
    /// <param name="mainWindow">Ссылка на главное окно приложения.</param>
    public UiEventsBinder(MainWindow mainWindow, MultiWindowControl multiWindow, TextEditorStatusViewModel statusBarViewModel)
    {
      _mainWindow = mainWindow;
      _multiWindow = multiWindow;
      _statusBarViewModel = statusBarViewModel;
    }

    /// <summary>
    /// Подписывает обработчики на события пользовательского интерфейса.
    /// </summary>
    public void Bind()
    {
      EventAggregator.Subscribe<EditorEvents.TextEditorActive>(e => OnTextEditorActive(e.IsActive));
      EventAggregator.Subscribe<EditorEvents.TextEditorActivated>(e => OnTextEditorActivated(e.ActiveEditor));
      EventAggregator.Subscribe<EditorEvents.TextEditorContainerClosing>(e => OnTextEditorClosing(e.IsClosing, e.EditorName));
      EventAggregator.Subscribe<EditorEvents.ActiveEditorChanged>(e => RefreshActiveEditorUiState());

      EventAggregator.Subscribe<SearchEvents.SearchWindowClosing>(e => OnSearchWindowClosing(e.IsClosing));
      EventAggregator.Subscribe<SearchEvents.SearchWindowActivated>(e => OnSearchWindowActivated(e.IsActive));

      EventAggregator.Subscribe<SearchEvents.SearchText>(e => SearchWindow_SearchTextHandler(e.SearchString, e.WholeWord, e.MatchCase, e.SearchArea, e.SearchParameters));
      EventAggregator.Subscribe<SearchEvents.ReplaceText>(e => SearchWindow_ReplaceTextHandler(e.ReplaceString, e.SearchString, e.WholeWord, e.MatchCase, e.SearchArea, e.SearchParameters));

      _mainWindow.SearchWindow.ClearHighlights += _multiWindow.OnSearchWindowClosing;

      EventAggregator.Subscribe<EditorEvents.TranslatorActive>(e => EventAggregator_TranslatorActive(e.IsActive));

      MenuHotkeyBinder.BindAutoRenumbering(_mainWindow.mainMenu);
    }

    private void EventAggregator_TranslatorActive(bool obj)
    {
      _mainWindow.StatusBar.Visibility = obj ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <summary>
    /// Обрабатывает событие активации текстового редактора, обновляя видимость связанных элементов интерфейса.
    /// </summary>
    /// <param name="isActive">Флаг, указывающий, активен ли редактор.</param>
    private void OnTextEditorActive(bool isActive)
    {
      _mainWindow.IsTextEditorActive = isActive;
      Visibility visibility = isActive ? Visibility.Visible : Visibility.Collapsed;

      _mainWindow.fileActionsSeparator.Visibility = visibility;
      _mainWindow.saveMenuItem.Visibility = visibility;
      _mainWindow.saveAsMenuItem.Visibility = visibility;
      _mainWindow.openFolderMenuItem.Visibility = visibility;
      _mainWindow.printMenuItem.Visibility = visibility;
      _mainWindow.searchMenuItem.Visibility = visibility;
      _mainWindow.searchReplaceMenuItem.Visibility = visibility;
      UpdateCompareMenuVisibility(isActive);
      UpdateArchiveMenuVisibility();
    }

    public void OnTextEditorActivated(UserControl editor)
    {
      if (editor is not TextEditorUI textEditorUI || textEditorUI.TextEditor == null)
        return;

      var textEditor = textEditorUI.TextEditor;

      if (_lastEditorControl != null)
      {
        _lastEditorControl.TextChanged -= OnTextChanged;
        _lastEditorControl.TextArea.Caret.PositionChanged -= OnCaretPositionChanged;
      }

      _lastEditorControl = textEditor;

      textEditor.TextChanged += OnTextChanged;
      textEditor.TextArea.Caret.PositionChanged += OnCaretPositionChanged;

      _statusBarViewModel.LineCount = textEditor.Document.LineCount;
      _statusBarViewModel.Line = textEditor.TextArea.Caret.Line;
      _statusBarViewModel.Column = textEditor.TextArea.Caret.Column;
      _statusBarViewModel.EncodingName = textEditorUI.TextEditorModel.Encoding?.WebName.ToUpperInvariant() ?? "UTF-8";
      UpdateCompareMenuVisibility(true);

      void OnTextChanged(object? sender, EventArgs e)
      {
        _statusBarViewModel.LineCount = textEditor.Document.LineCount;
      }

      void OnCaretPositionChanged(object? sender, EventArgs e)
      {
        _statusBarViewModel.Line = textEditor.TextArea.Caret.Line;
        _statusBarViewModel.Column = textEditor.TextArea.Caret.Column;
      }
    }

    /// <summary>
    /// Обрабатывает событие закрытия редактора. Если он был активен — отключает его.
    /// </summary>
    /// <param name="isActive">Флаг активности редактора.</param>
    /// <param name="name">Имя закрываемого редактора.</param>
    private void OnTextEditorClosing(bool isActive, string name)
    {
      RefreshActiveEditorUiState();
    }

    /// <summary>
    /// Обрабатывает событие закрытия окна поиска.
    /// </summary>
    /// <param name="closing">Флаг, указывающий, закрыто ли окно поиска.</param>
    private void OnSearchWindowClosing(bool closing)
    {
      IsSearchWindowOpen = closing;
    }

    /// <summary>
    /// Обрабатывает событие активации окна поиска.
    /// Также удаляет подписку на событие поиска текста.
    /// </summary>
    /// <param name="activated">Флаг, указывающий, активировано ли окно поиска.</param>
    private void OnSearchWindowActivated(bool activated)
    {
      IsSearchWindowOpen = activated;
      EventAggregator.Unsubscribe<SearchEvents.SearchText>(e => SearchWindow_SearchTextHandler(e.SearchString, e.WholeWord, e.MatchCase, e.SearchArea, e.SearchParameters));
    }

    /// <summary>
    /// Обрабатывает событие поиска текста и передаёт параметры в <see cref="MultiWindowControl"/>.
    /// </summary>
    /// <param name="searchText">Текст для поиска.</param>
    /// <param name="wholeWord">Флаг поиска только целых слов.</param>
    /// <param name="caseWord">Флаг учёта регистра.</param>
    /// <param name="searchArea">Область, в которой выполняется поиск.</param>
    /// <param name="searchParameters">Дополнительные параметры поиска.</param>
    public void SearchWindow_SearchTextHandler(string searchText, bool? wholeWord, bool? caseWord, int searchArea, string searchParameters)
    {
      _multiWindow.SearchData(searchText, wholeWord, caseWord, searchArea, searchParameters);
    }

    private void SearchWindow_ReplaceTextHandler(string replaceText, string searchText, bool? wholeWord, bool? caseWord, int searchArea, string searchParameters)
    {
      _multiWindow.ReplaceData(replaceText, searchText, wholeWord, caseWord, searchArea, searchParameters);
    }

    private void RefreshActiveEditorUiState()
    {
      Application.Current.Dispatcher.BeginInvoke(
        DispatcherPriority.Background,
        new Action(() =>
        {
          var activeEditor = _multiWindow.GetActiveTextEditor();
          bool isActive = activeEditor != null;

          OnTextEditorActive(isActive);
          UpdateArchiveMenuVisibility();
          if (activeEditor != null)
          {
            OnTextEditorActivated(activeEditor);
          }
        }));
    }

    private void UpdateArchiveMenuVisibility()
    {
      var isArchiveControlActive = _multiWindow.GetActiveWorkspaceControl() is ArchiveControl;
      _mainWindow.createArchiveMenuItem.Visibility = isArchiveControlActive
        ? Visibility.Visible
        : Visibility.Collapsed;
      _mainWindow.downloadArchivesMenuItem.Visibility = isArchiveControlActive
        ? Visibility.Visible
        : Visibility.Collapsed;
      _mainWindow.uploadArchiveMenuItem.Visibility = isArchiveControlActive
        ? Visibility.Visible
        : Visibility.Collapsed;
    }

    private void UpdateCompareMenuVisibility(bool isTextEditorActive)
    {
      if (_mainWindow.compareMenuItem == null)
      {
        return;
      }

      var openTextEditorsCount = _multiWindow.GetOpenTextEditors().Count;
      _mainWindow.compareMenuItem.Visibility = isTextEditorActive && openTextEditorsCount > 1
        ? Visibility.Visible
        : Visibility.Collapsed;
    }
  }
}
