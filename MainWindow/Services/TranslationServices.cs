using AppConfiguration.Base;
using ControlCommandAnalyser;
using ControlCommandAnalyser.Model.Ok;
using Message;
using System.Windows;
using UI.Controls;
using UI.Controls.Runner;
using UI.Controls.TextEditor;
using UI.Windows.WpfDocking.Windows.Docking;

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
    /// Сервис для работы с файлами.
    /// </summary>
    private readonly FileService _fileService;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="AdminServices"/>.
    /// </summary>
    /// <param name="multiWindow">Сервис управления многооконным интерфейсом.</param>
    /// <param name="fileService">Сервис  для работы с файлами.</param>
    public TranslationServices(MultiWindowService multiWindow, FileService fileService)
    {
      _multiWindow = multiWindow;
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
      var editor = await _multiWindow.GetActiveTextEditor(EditorType.TextEditor);
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
      var editor = await _multiWindow.GetActiveTextEditor(EditorType.TextEditor);
      var container = await _multiWindow.GetActiveTextEditorContainer(EditorType.Translator);
      if (container == null)
      {
        await BuildAsync();
        editor = await _multiWindow.GetActiveTextEditor(EditorType.TextEditor);
        container = await _multiWindow.GetActiveTextEditorContainer(EditorType.Translator);
      }


      if (container == null)
      {
        MessageBoxCustom.Show($"Не удалось запустить исполнитель программы контроля.", "Ошибка запуска программы контроля", image: MessageBoxImage.Error);
        return;
      }

      var dockManager = container.GetDockControl();
      if (dockManager == null) return;

      DockItem? foundDockItem = null;

      // Ждём, пока хотя бы один DockItem появится
      for (int i = 0; i < 500; i++) // максимум 500 мс
      {
        if (dockManager.DockItems.Count > 0)
        {
          foundDockItem = dockManager.DockItems.FirstOrDefault(item => item.IsActiveItem == true);
          if (foundDockItem != null)
          {
            break;
          }
        }
        await Task.Delay(10);
      }

      if (foundDockItem?.Content is not TranslatorItem translator) return;

      editor = translator.GetRightEditor();
      if (editor == null)
      {
        ShowEditorNotFoundError();
        return;
      }

      if (translator.ErrorCount > 0)
      {
        MessageBoxCustom.Show($"Возникли ошибки сборки ({translator.ErrorCount} ошибок). Устраните ошибки и повторите попытку.", "Ошибка запуска программы контроля", image: MessageBoxImage.Error);
        return;
      }

      var models = translator.TranslationModels;

      await _multiWindow.DeleteTranslatorItem(translator, EditorType.Translator);

      RunControl runControl = new RunControl();
      runControl.SetLeftEditor(editor);
      var foundItem = models.FirstOrDefault(item => item.GetType() == typeof(OkCommandModel));
      if (foundItem != null && foundItem is OkCommandModel okCommandModel)
      {
        runControl.FileName = okCommandModel.ObjectCode;
      }

      await _multiWindow.AddRunItem(runControl, EditorType.Run);

      runControl.Start(models);
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
        EventAggregator.RaiseTextEditorContainerClosing(true, editor.TextEditorModel.FileName);
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
      MessageBoxCustom.Show("Редактор не найден", "Ошибка", MessageBoxButton.OK, image: MessageBoxImage.Error);
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
        translateEditor.TextEditorModel.FilePath = editor.TextEditorModel.FilePath;
        var manager = new CommandTranslationManager();
        var models = manager.ParseAllAndDisplay(text, translateEditor);

        EventAggregator.RaiseTextEditorActivated(editor);

        var item = await _multiWindow.AddTranslatorItem(editor, translateEditor, EditorType.Translator);
        item.TranslationModels = models;
      }
    }
  }
}
