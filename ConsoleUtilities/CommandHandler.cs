using System.Diagnostics;
using System.Text;

namespace ConsoleUtilities
{
  /// <summary>
  /// Обрабатывает команды, вводимые в консоль, и выполняет соответствующие действия.
  /// </summary>
  public class CommandHandler
  {
    private bool _isListening = true;
    private StringBuilder _inputBuffer = new StringBuilder();
    private readonly object _lock = new object();
    private StringBuilder _consoleLog = new StringBuilder(); // Буфер для хранения вывода консоли

    private readonly Dictionary<string, Action<string[]>> _commands;

    /// <summary>
    /// Событие изменения режима администратора.
    /// </summary>
    public event EventHandler<bool> AdminModeChanged;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="CommandHandler"/>.
    /// Настраивает перехват вывода консоли и запускает прослушивание ввода.
    /// </summary>
    public CommandHandler()
    {
      Console.TreatControlCAsInput = true;
      _commands = new Dictionary<string, Action<string[]>>(StringComparer.OrdinalIgnoreCase)
      {
        { "exit", args => ExitApplication() },
        { "clear", args => ClearConsole() },
        { "help", args => ShowHelp() },
        { "showtable", args => ShowTable(args) },
        { "save", args => SaveConsoleLog() },
        { "clearlogs", args => ClearLogs() },
        { "setAdmin", args => SetAdminMode(true) },
        { "delAdmin", args => SetAdminMode(false) },
      };

      Console.SetOut(new ConsoleWriter(Console.Out, _consoleLog));

      StartInputListener();
    }

    /// <summary>
    /// Запускает асинхронный процесс прослушивания ввода с консоли.
    /// </summary>
    private void StartInputListener()
    {
      Task.Run(async () =>
      {
        while (_isListening)
        {
          await ReadKeyWithHotkey();
        }
      });
    }

    /// <summary>
    /// Асинхронно считывает нажатые клавиши и обрабатывает горячие клавиши.
    /// </summary>
    /// <returns>Задача, представляющая асинхронную операцию.</returns>
    private async Task ReadKeyWithHotkey()
    {
      while (!Console.KeyAvailable)
      {
        await Task.Delay(1);
      }

      ConsoleKeyInfo keyInfo = Console.ReadKey(true);

      lock (_lock)
      {
        // Обработка горячей клавиши: Ctrl + `
        if (keyInfo.Key == ConsoleKey.Oem3 && keyInfo.Modifiers.HasFlag(ConsoleModifiers.Control))
        {
          ConsoleManager.Instance.ToggleConsole();
          return;
        }

        // Если нажата клавиша Enter, выполняем команду
        if (keyInfo.Key == ConsoleKey.Enter)
        {
          string command = _inputBuffer.ToString().Trim();
          _inputBuffer.Clear();
          Console.WriteLine();
          Task.Run(() => ExecuteCommand(command));
          return;
        }

        // Обработка клавиши Backspace
        if (keyInfo.Key == ConsoleKey.Backspace && _inputBuffer.Length > 0)
        {
          _inputBuffer.Remove(_inputBuffer.Length - 1, 1);
          Console.Write("\b \b");
          return;
        }

        // Если введен обычный символ, добавляем его в буфер и выводим на экран
        if (!char.IsControl(keyInfo.KeyChar))
        {
          _inputBuffer.Append(keyInfo.KeyChar);
          Console.Write(keyInfo.KeyChar);
        }
      }
    }

    /// <summary>
    /// Выполняет команду, введенную пользователем, определяя и вызывая соответствующее действие.
    /// </summary>
    /// <param name="command">Команда, введенная пользователем.</param>
    /// <returns>Задача, представляющая асинхронную операцию выполнения команды.</returns>
    private async Task ExecuteCommand(string command)
    {
      if (string.IsNullOrWhiteSpace(command))
      {
        return;
      }

      string[] parts = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
      string commandName = parts[0];
      string[] args = parts.Skip(1).ToArray();

      if (_commands.TryGetValue(commandName, out Action<string[]> action))
      {
        action.Invoke(args);
      }
      else
      {
        Utilities.LoggerUtility.LogDebug($"Команда \"{command}\" передана в cmd. Ожидайте ответа...");
        await ExecuteInCmd(command);
        Utilities.LoggerUtility.LogDebug($"Команда \"{command}\" выполнена. Можете продолжать работу");
      }
    }

    /// <summary>
    /// Отображает список доступных команд в консоли.
    /// </summary>
    private void ShowHelp()
    {
      Console.WriteLine("Доступные команды:");
      foreach (var command in _commands.Keys)
      {
        Console.WriteLine($" - {command}");
      }
    }

    /// <summary>
    /// Завершает работу приложения.
    /// </summary>
    private void ExitApplication()
    {
      Console.WriteLine("Выход из программы...");
      _isListening = false;
      Environment.Exit(0);
    }

    /// <summary>
    /// Очищает консоль и сбрасывает буфер логов.
    /// </summary>
    private void ClearConsole()
    {
      Console.Clear();
      _consoleLog.Clear();
    }

    /// <summary>
    /// Удаляет все файлы логов из каталога логов.
    /// </summary>
    private void ClearLogs()
    {
      string logDirectory = Path.Combine(AppContext.BaseDirectory, "logs");

      if (!Directory.Exists(logDirectory))
      {
        Console.WriteLine("Логи отсутствуют.");
        return;
      }

      try
      {
        var logFiles = Directory.GetFiles(logDirectory, "*.log");

        if (logFiles.Length == 0)
        {
          Console.WriteLine("Нет логов для удаления.");
          return;
        }

        foreach (var file in logFiles)
        {
          File.Delete(file);
        }

        Console.WriteLine("Все файлы логов успешно удалены.");
        Utilities.LoggerUtility.LogWarning("Все логи были удалены пользователем.");
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Ошибка при удалении логов: {ex.Message}");
        Utilities.LoggerUtility.LogError($"Ошибка при удалении логов - {ex}");
      }
    }

    /// <summary>
    /// Отображает содержимое указанной таблицы базы данных.
    /// Если имя таблицы не указано, выводит список доступных таблиц.
    /// </summary>
    /// <param name="args">Массив аргументов, где первый элемент — имя таблицы.</param>
    private void ShowTable(string[] args)
    {
      using (var db = AppConfig.Config.SystemStateManager.Context)
      {
        if (args.Length == 0)
        {
          Console.WriteLine("Использование: showtable [TableName]");
          Console.WriteLine("\nДоступные таблицы:");

          var tableNames = new List<string>
          {
            "breakdowntester",
            "chassismanager",
            "fastmeter",
            "powersourcemodule",
            "precisionmeter",
            "rack",
            "relayswitchmodule",
            "switchingdevice",
          };

          foreach (var table in tableNames)
          {
            Console.WriteLine($" - {table}");
          }

          return;
        }

        string tableName = args[0];

        switch (tableName.ToLower())
        {
          case "breakdowntester":
            TableFormatter.DisplayTable(db.BreakdownTesters.ToList());
            break;
          case "chassismanager":
            TableFormatter.DisplayTable(db.ChassisManagers.ToList());
            break;
          case "fastmeter":
            TableFormatter.DisplayTable(db.FastMeters.ToList());
            break;
          case "powersourcemodule":
            TableFormatter.DisplayTable(db.PowerSourceModules.ToList());
            break;
          case "precisionmeter":
            TableFormatter.DisplayTable(db.PrecisionMeters.ToList());
            break;
          case "rack":
            TableFormatter.DisplayTable(db.Rack.ToList());
            break;
          case "relayswitchmodule":
            TableFormatter.DisplayTable(db.RelaySwitchModules.ToList());
            break;
          case "switchingdevice":
            TableFormatter.DisplayTable(db.SwitchingDevices.ToList());
            break;
          default:
            Console.WriteLine($"Таблица '{tableName}' не найдена.");
            break;
        }
      }
    }

    /// <summary>
    /// Сохраняет текущий вывод консоли в текстовый файл.
    /// </summary>
    private void SaveConsoleLog()
    {
      if (_consoleLog.Length == 0)
      {
        Console.WriteLine("Консоль пуста, нечего сохранять.");
        return;
      }

      string saveDirectory = AppConfig.FileLocations.ConsoleSaveDirectory;
      if (string.IsNullOrWhiteSpace(saveDirectory))
      {
        Console.WriteLine("Ошибка: ConsoleSaveDirectory не задан.");
        return;
      }

      try
      {
        Directory.CreateDirectory(saveDirectory);

        string fileName = $"ConsoleLog_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt";
        string filePath = Path.Combine(saveDirectory, fileName);

        File.WriteAllText(filePath, _consoleLog.ToString());

        Console.WriteLine($"Лог консоли сохранен: {filePath}");
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Ошибка сохранения лога: {ex.Message}");
      }
    }

    /// <summary>
    /// Выполняет указанную команду в командной строке (cmd) и выводит результат.
    /// </summary>
    /// <param name="command">Команда для выполнения.</param>
    /// <returns>Задача, представляющая асинхронную операцию выполнения команды.</returns>
    private async Task ExecuteInCmd(string command)
    {
      ProcessStartInfo psi = new ProcessStartInfo
      {
        FileName = "cmd.exe",
        Arguments = $"/C {command}",
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        CreateNoWindow = true,
      };

      try
      {
        using (Process process = new Process { StartInfo = psi })
        {
          process.Start();
          string output = await process.StandardOutput.ReadToEndAsync();
          string error = await process.StandardError.ReadToEndAsync();

          if (!string.IsNullOrWhiteSpace(output))
          {
            Console.WriteLine(output);
          }

          if (!string.IsNullOrWhiteSpace(error))
          {
            Console.WriteLine($"Ошибка: {error}");
          }
        }
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Ошибка выполнения команды: {ex.Message}");
      }
    }

    /// <summary>
    /// Устанавливает режим администратора.
    /// </summary>
    /// <param name="enable">
    /// Если <c>true</c>, включает режим администратора; если <c>false</c> — отключает.
    /// </param>
    private void SetAdminMode(bool enable)
    {
      if (enable)
      {
        Utilities.LoggerUtility.LogInformation("Включение режима администратора.");
        AdminModeChanged?.Invoke(null, true);
      }
      else
      {
        Utilities.LoggerUtility.LogInformation("Отключение режима администратора.");
        AdminModeChanged?.Invoke(null, false);
      }
    }
  }

  /// <summary>
  /// Перехватывает вывод консоли, записывая его в дополнительный лог.
  /// </summary>
  public class ConsoleWriter : TextWriter
  {
    private readonly TextWriter _originalConsole;
    private readonly StringBuilder _consoleLog;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="ConsoleWriter"/>.
    /// </summary>
    /// <param name="originalConsole">Исходный поток вывода консоли.</param>
    /// <param name="consoleLog">Буфер для хранения логов консоли.</param>
    public ConsoleWriter(TextWriter originalConsole, StringBuilder consoleLog)
    {
      _originalConsole = originalConsole;
      _consoleLog = consoleLog;
    }

    /// <summary>
    /// Записывает символ в исходный поток и добавляет его в лог.
    /// </summary>
    /// <param name="value">Символ для записи.</param>
    public override void Write(char value)
    {
      _originalConsole.Write(value);
      _consoleLog.Append(value);
    }

    /// <summary>
    /// Записывает строку в исходный поток и добавляет её в лог.
    /// </summary>
    /// <param name="value">Строка для записи.</param>
    public override void Write(string value)
    {
      _originalConsole.Write(value);
      _consoleLog.Append(value);
    }

    /// <summary>
    /// Возвращает используемую кодировку.
    /// </summary>
    public override Encoding Encoding => Encoding.UTF8;
  }
}
