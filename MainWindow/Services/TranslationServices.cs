using AppConfiguration.Base;
using ControlCommandAnalyser;
using DevZest.Windows.Docking;
using ICSharpCode.AvalonEdit;
using System.Windows;
using UI.Components;
using UI.Components.MultiEditorMethods;
using UI.Controls;
using UI.Controls.TextEditor;
using static System.Net.Mime.MediaTypeNames;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace MainWindowProgram.Services
{
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

    private readonly FileService _fileService;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="AdminServices"/>.
    /// </summary>
    /// <param name="mainWindow">Главное окно приложения.</param>
    /// <param name="multiWindow">Сервис управления многооконным интерфейсом.</param>
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
    public async Task StartTranslationAsync()
    {
      TextEditorUI editor = await _multiWindow.GetActiveTextEditor();
      var translationContainer = await _multiWindow.GetActiveTextEditorContainer(EditorType.Translator);

      if (editor == null && translationContainer != null)
      {
        var dockManager = translationContainer.GetDockControl();
        if (dockManager != null)
        {
          var foundDockItem = dockManager.DockItems.FirstOrDefault(item => item.IsActiveItem == true);
          if (foundDockItem != null && foundDockItem.Content is TranslatorItem)
          {
            editor = (foundDockItem.Content as TranslatorItem).GetLeftEditor();
            if (editor == null)
            {
              MessageBox.Show("Редактор не найден", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
              return;
            }
            else
            {
              EditExistingTranslator(editor, foundDockItem);

            }
          }
        }
      }
      else
      {
        string text = editor.Text;

        if (_multiWindow.RemoveActiveTextEditor(true))
        {
          await CreateNewTranslator(editor, text);
        }
      }
    }



    private void EditExistingTranslator(TextEditorUI editor, DockItem foundDockItem)
    {
      string text = editor.Text;
      var translateEditor = _fileService.CreateTranslationFileAsync();

      var manager = new CommandTranslationManager();
      var models = manager.ParseAllAndDisplay(text, translateEditor);
      var item = (foundDockItem.Content as TranslatorItem);
      item.SetRightEditor(translateEditor);
      item.SetRightEditorName(translateEditor.TextEditorModel.FileName);
      item.ErrorClear();
      foreach (var model in models)
      {
        if (model.Errors.Count > 0)
        {
          item.SetError(model.Errors);
        }
      }
    }

    private async Task CreateNewTranslator(TextEditorUI editor, string text)
    {
      var translateEditor = _fileService.CreateTranslationFileAsync();
      if (translateEditor != null)
      {
        var manager = new CommandTranslationManager();
        var models = manager.ParseAllAndDisplay(text, translateEditor);
        EventAggregator.RaiseTextEditorActivated(editor);

        var item = await _multiWindow.AddTranslatorItem(editor, translateEditor, EditorType.Translator);
        item.ErrorClear();
        foreach (var model in models)
        {
          if (model.Errors.Count > 0)
          {
            item.SetError(model.Errors);
          }
        }
      }
    }
  }
}
