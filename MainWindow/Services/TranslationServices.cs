using System.ComponentModel;
using System.Windows;
using AppConfiguration.Base;
using ControlCommandAnalyser;
using DevZest.Windows.Docking;
using ICSharpCode.AvalonEdit;
using UI.Components;
using UI.Components.MultiEditorMethods;
using UI.Controls;
using UI.Controls.Message;
using UI.Controls.TextEditor;
using static System.Net.Mime.MediaTypeNames;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using static UI.Controls.Message.MessageBox;

namespace MainWindowProgram.Services
{
  /// <summary>
  /// Сервис трансляции команд из текстового редактора.
  /// Обеспечивает распознавание команд, отображение результатов трансляции и работу с двумя редакторами: исходным и переводом.
  /// </summary>
  public class TranslationServices
  {
    /// <summary>
    /// Сервис для управления многооконным интерфейсом.
    /// </summary>
    private readonly MultiWindowService _multiWindow;

    /// <summary>
    /// Ссылка на главное окно приложения.
    /// </summary>
    private readonly MainWindow _mainWindow;

    /// <summary>
    /// Сервис для работы с файлами.
    /// </summary>
    private readonly FileService _fileService;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="AdminServices"/>.
    /// </summary>
    /// <param name="mainWindow">Главное окно приложения.</param>
    /// <param name="multiWindow">Сервис управления многооконным интерфейсом.</param>
    /// <param name="fileService">Сервис  для работы с файлами.</param>
    public TranslationServices(MainWindow mainWindow, MultiWindowService multiWindow, FileService fileService)
    {
      _multiWindow = multiWindow;
      _mainWindow = mainWindow;
      _fileService = fileService;
    }

    /// <summary>
    /// Запускает процесс трансляции текущего открытого текста из редактора.
    /// Выполняет распознавание команд, логирует результат и применяет подсветку
    /// в соответствии с успешностью распознавания.
    /// </summary>
    /// <returns>Задача, представляющая асинхронную операцию трансляции.</returns>
    public async Task BuildAsync()
    {
      var editor = await _multiWindow.GetActiveTextEditor();
      var translationContainer = await _multiWindow.GetActiveTextEditorContainer(EditorType.Translator);

      if (editor == null && translationContainer != null)
      {
        await TryUpdateExistingTranslator(translationContainer);
      }
      else if (editor != null)
      {
        await TryCreateNewTranslator(editor);
      }
      else
      {
        ShowEditorNotFoundError();
      }
    }

    /// <summary>
    /// Запускает процесс трансляции текущего открытого текста из редактора.
    /// Выполняет распознавание команд, логирует результат и применяет подсветку
    /// в соответствии с успешностью распознавания.
    /// </summary>
    /// <returns>Задача, представляющая асинхронную операцию трансляции.</returns>
    public async Task RunAsync()
    {
      var editor = await _multiWindow.GetActiveTextEditor();
      var container = await _multiWindow.GetActiveTextEditorContainer(EditorType.Translator);
      var dockManager = container.GetDockControl();
      if (dockManager == null) return;

      var foundDockItem = dockManager.DockItems.FirstOrDefault(item => item.IsActiveItem == true);
      if (foundDockItem?.Content is not TranslatorItem translator) return;

      editor = translator.GetRightEditor();
      if (editor == null)
      {
        ShowEditorNotFoundError();
        return;
      }
    }

    /// <summary>
    /// Пытается создать новый транслятор, используя текст из указанного редактора.
    /// </summary>
    /// <param name="editor">Редактор с исходным текстом.</param>
    private async Task TryCreateNewTranslator(TextEditorUI editor)
    {
      string text = editor.Text;

      if (_multiWindow.RemoveActiveTextEditor(true))
      {
        await CreateNewTranslator(editor, text);
      }
    }

    /// <summary>
    /// Пытается обновить существующий транслятор, если активен соответствующий элемент интерфейса.
    /// </summary>
    /// <param name="container">Контейнер, содержащий редактор трансляции.</param>
    private async Task TryUpdateExistingTranslator(TextEditorContainer container)
    {
      var dockManager = container.GetDockControl();
      if (dockManager == null) return;

      var foundDockItem = dockManager.DockItems.FirstOrDefault(item => item.IsActiveItem == true);
      if (foundDockItem?.Content is not TranslatorItem translator) return;

      var editor = translator.GetLeftEditor();
      if (editor == null)
      {
        ShowEditorNotFoundError();
        return;
      }

      EditExistingTranslator(editor, foundDockItem);
    }

    /// <summary>
    /// Выводит сообщение об ошибке, если редактор не найден.
    /// </summary>
    private void ShowEditorNotFoundError()
    {
      Show(UI.Controls.Message.MessageBox.Status.Error, "Редактор не найден", "Ошибка", MessageBoxButton.OK);
    }

    /// <summary>
    /// Обновляет существующий компонент транслятора на основе текста из заданного редактора.
    /// Выполняет трансляцию команд и выводит результат во второй (правый) редактор.
    /// </summary>
    /// <param name="editor">Редактор с исходным текстом.</param>
    /// <param name="foundDockItem">Док-элемент, содержащий компонент транслятора.</param>
    private void EditExistingTranslator(TextEditorUI editor, DockItem foundDockItem)
    {
      string text = editor.Text;
      var translateEditor = _fileService.CreateTranslationFileAsync();

      var manager = new CommandTranslationManager();
      var models = manager.ParseAllAndDisplay(text, translateEditor);

      if (foundDockItem.Content is TranslatorItem item)
      {
        item.SetRightEditor(translateEditor);
        item.SetRightEditorName(translateEditor.TextEditorModel.FileName);
        item.TranslationModels = models;
      }
    }

    /// <summary>
    /// Создаёт новый компонент транслятора, содержащий редактор исходного текста и редактор результата трансляции.
    /// Выполняет разбор команд и отображает результат трансляции.
    /// </summary>
    /// <param name="editor">Редактор с исходным текстом.</param>
    /// <param name="text">Текст, подлежащий трансляции.</param>
    /// <returns>Асинхронная задача создания компонента транслятора.</returns>
    private async Task CreateNewTranslator(TextEditorUI editor, string text)
    {
      var translateEditor = _fileService.CreateTranslationFileAsync();
      if (translateEditor != null)
      {
        var manager = new CommandTranslationManager();
        var models = manager.ParseAllAndDisplay(text, translateEditor);

        EventAggregator.RaiseTextEditorActivated(editor);

        var item = await _multiWindow.AddTranslatorItem(editor, translateEditor, EditorType.Translator);
        item.TranslationModels = models;
      }
    }
  }
}
