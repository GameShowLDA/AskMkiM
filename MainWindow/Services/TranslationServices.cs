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

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="AdminServices"/>.
    /// </summary>
    /// <param name="mainWindow">Главное окно приложения.</param>
    /// <param name="multiWindow">Сервис управления многооконным интерфейсом.</param>
    public TranslationServices(MainWindow mainWindow, MultiWindowService multiWindow)
    {
      _multiWindow = multiWindow;
      _mainWindow = mainWindow;
    }

    /// <summary>
    /// Открывает элемент управления для работы с программируемой пробойной установкой (ППУ).
    /// </summary>
    /// <returns>Задача, представляющая асинхронную операцию.</returns>
    public async Task StartTranslationAsync()
    {
      var editor = await _multiWindow.GetActiveTextEditor();

      if (editor == null)
      {
        return;
      }

      string text = editor.Text;

      var translator = new TranslationManager();
      await translator.Translate(text);
    }
  }
}
