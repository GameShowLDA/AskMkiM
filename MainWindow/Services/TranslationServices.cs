using ControlCommandAnalyser;
using System.Windows;
using UI.Components.MultiEditorMethods;
using UI.Controls;
using UI.Controls.TextEditor;
using static System.Net.Mime.MediaTypeNames;

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
      var translationContainer = await _multiWindow.GetActiveTranslateContainer();
      if (editor == null)
      {
        editor = translationContainer.GetLeftEditor();
        if (editor == null)
        {
          MessageBox.Show("Редактор не найден", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
          return;
        }
        else
        {
          string text = editor.Text;
          var translateEditor = _fileService.CreateTranslationFileAsync();

          var manager = new CommandTranslationManager();
          var models = manager.ParseAllAndDisplay(text, translateEditor);

          if (translationContainer != null)
          {
            translationContainer.SetRighttEditor(translateEditor);
          }
        }
      }
      else
      {
        string text = editor.Text;
        if (_multiWindow.RemoveActiveTextEditor())
        {
          var translateEditor = _fileService.CreateTranslationFileAsync();

          var manager = new CommandTranslationManager();
          var models = manager.ParseAllAndDisplay(text, translateEditor);

          if (translationContainer != null)
          {
            translationContainer.SetLeftEditor(editor);
            translationContainer.SetRighttEditor(translateEditor);
          }
        }
        else
        {
          throw new Exception("Не найдено активное окно при трансляции");
        }
      }
    }
  }
}
