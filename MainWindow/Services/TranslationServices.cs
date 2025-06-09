using System.Windows;
using ControlCommandAnalyser;

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
      var editor = await _multiWindow.GetActiveTextEditor();
      if (editor == null)
      {
        MessageBox.Show("Редактор не найден", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        return;
      }
      string text = editor.Text;

      await _fileService.CreateTranslationFileAsync();
      editor = await _multiWindow.GetActiveTextEditor();


      var manager = new CommandTranslationManager();
      var models = manager.ParseAllAndDisplay(text, editor);

      foreach (var model in models)
      {
        Console.WriteLine($"{model.CommandNumber} {model.Mnemonic}");

        // Вывести все публичные свойства (кроме CommandNumber и Mnemonic, чтобы не дублировать)
        var props = model.GetType().GetProperties()
            .Where(p => p.Name != "CommandNumber" && p.Name != "Mnemonic");

        foreach (var prop in props)
        {
          var value = prop.GetValue(model);

          // Спец-вывод для коллекций
          if (value is IDictionary<string, string> dict)
          {
            Console.WriteLine($"  {prop.Name}:");
            foreach (var kv in dict)
              Console.WriteLine($"    {kv.Key} => {kv.Value}");
          }
          else if (value is IEnumerable<string> list && !(value is string))
          {
            Console.WriteLine($"  {prop.Name}:");
            foreach (var item in list)
              Console.WriteLine($"    {item}");
          }
          else
          {
            Console.WriteLine($"  {prop.Name}: {value}");
          }
        }
      }

    }
  }
}
