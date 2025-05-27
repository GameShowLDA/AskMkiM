using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static UI.Components.Invoke.OpenFileButton;
using UI.Controls.GPT;
using System.Windows;
using ControlCommandAnalyser.Translation;

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
      // Создаем файл для трансляции

      var editor = await _multiWindow.GetActiveTextEditor();
      if (editor == null)
      {
        MessageBox.Show("Редактор не найден", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        return;
      }
      string text = editor.Text;

      await _fileService.CreateTranslationFileAsync();
      editor = await _multiWindow.GetActiveTextEditor();
      editor.Text = text;

      var translator = new TranslationManager
      {
        HighlightCallback = editor.ApplyHighlighting
      };

      await translator.Translate(text);
    }

  }
}
