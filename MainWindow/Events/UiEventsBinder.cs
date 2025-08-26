using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Effects;
using AppConfiguration.Base;
using ICSharpCode.AvalonEdit;
using MainWindowProgram.HotkeyBindings;
using MainWindowProgram.Services;
using MainWindowProgram.ViewModels;
using UI.Components;
using UI.Components.FileComparerControls;
using UI.Controls.Search;
using UI.Controls.TextEditor;
using static UI.Components.Invoke.OpenFileButton;

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
      EventAggregator.TextEditorActive += OnTextEditorActive;
      EventAggregator.TextEditorActivated += OnTextEditorActivated;
      EventAggregator.TextEditorContainerClosing += OnTextEditorClosing;

      EventAggregator.SearchWindowClosing += OnSearchWindowClosing;
      EventAggregator.SearchWindowAtivated += OnSearchWindowActivated;

      EventAggregator.SearchText += SearchWindow_SearchTextHandler;

      EventAggregator.ReplaceText += SearchWindow_ReplaceTextHandler;

      _mainWindow.SearchWindow.ClearHighlights += _multiWindow.OnSearchWindowClosing;

      //EventAggregator.OpenOpk += OnOpenOpk;

      EventAggregator.CompareFiles += OnCompareFiles;
      EventAggregator.TranslatorActive += EventAggregator_TranslatorActive;

      MenuHotkeyBinder.BindAutoRenumbering(_mainWindow.mainMenu);
    }

    private void EventAggregator_TranslatorActive(bool obj)
    {
      _mainWindow.StatusBar.Visibility = obj ? Visibility.Visible : Visibility.Collapsed;
    }

    private void OnCompareFiles(string firstFilePath, string secondFilePath)
    {
      var firstFileName = Path.GetFileName(firstFilePath);
      var secondFileName = Path.GetFileName(secondFilePath);
      var fileCompareControl = new FileCompareControl(firstFilePath, secondFilePath);
      _multiWindow.AddControl($"{firstFileName}/{secondFileName}", fileCompareControl, TypeWindow.Files);
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
      if (isActive)
      {
        OnTextEditorActive(false);
      }
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
      EventAggregator.SearchText -= SearchWindow_SearchTextHandler;
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
  }
}
