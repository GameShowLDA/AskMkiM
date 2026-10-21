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
      UpdateCompareMenuVisibility(isActive);
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









    private void RefreshActiveEditorUiState()
    {
      Application.Current.Dispatcher.BeginInvoke(
        DispatcherPriority.Background,
        new Action(() =>
        {
          _mainWindow.RefreshSearchTarget();
          var activeEditor = _multiWindow.GetActiveTextEditor();
          bool isActive = activeEditor != null;

          OnTextEditorActive(isActive);
          if (activeEditor != null)
          {
            OnTextEditorActivated(activeEditor);
          }
        }));
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
