using System.Diagnostics;
using System.Text;

namespace ConsoleUtilities
{
  public class CommandHandler
  {
    private bool _isListening = true;
    private StringBuilder _inputBuffer = new StringBuilder();
    private object _lock = new object();
    private StringBuilder _consoleLog = new StringBuilder(); // Буфер для хранения вывода консоли

    private readonly Dictionary<string, Action<string[]>> _commands;

    /// <summary>
    /// Событие изменения режима администратора вручную.
    /// </summary>
    public event EventHandler<bool> AdminModeChanged;

    public CommandHandler()
    {
      Console.TreatControlCAsInput = true; // Позволяет обрабатывать Ctrl
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

      // Перехватываем весь вывод в консоль
      Console.SetOut(new ConsoleWriter(Console.Out, _consoleLog));

      StartInputListener();
    }

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

    private async Task ReadKeyWithHotkey()
    {
      while (!Console.KeyAvailable) { await Task.Delay(1); }

      ConsoleKeyInfo keyInfo = Console.ReadKey(true);

      lock (_lock)
      {
        if (keyInfo.Key == ConsoleKey.Oem3 && keyInfo.Modifiers.HasFlag(ConsoleModifiers.Control))
        {
          ConsoleManager.Instance.ToggleConsole();
          return;
        }

        if (keyInfo.Key == ConsoleKey.Enter)
        {
          string command = _inputBuffer.ToString().Trim();
          _inputBuffer.Clear();
          Console.WriteLine();
          Task.Run(() => ExecuteCommand(command));
          return;
        }

        if (keyInfo.Key == ConsoleKey.Backspace && _inputBuffer.Length > 0)
        {
          _inputBuffer.Remove(_inputBuffer.Length - 1, 1);
          Console.Write("\b \b");
          return;
        }

        if (!char.IsControl(keyInfo.KeyChar))
        {
          _inputBuffer.Append(keyInfo.KeyChar);
          Console.Write(keyInfo.KeyChar);
        }
      }
    }

    private async Task ExecuteCommand(string command)
    {
      if (string.IsNullOrWhiteSpace(command))
        return;

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


    private void ShowHelp()
    {
      Console.WriteLine("Доступные команды:");
      foreach (var command in _commands.Keys)
      {
        Console.WriteLine($" - {command}");
      }
    }

    private void ExitApplication()
    {
      Console.WriteLine("Выход из программы...");
      _isListening = false;
      Environment.Exit(0);
    }

    /// <summary>
    /// Очищает консоль и сбрасывает лог.
    /// </summary>
    private void ClearConsole()
    {
      Console.Clear();
      _consoleLog.Clear(); // Очищаем лог
    }

    /// <summary>
    /// Удаляет все файлы логов.
    /// </summary>
    private void ClearLogs()
    {
      string logDirectory = Path.Combine(AppContext.BaseDirectory, "logs"); // Директория логов

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
                "switchingdevice"
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
    /// Сохраняет весь текст из консоли в файл.
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
    /// Выполняет неизвестную команду в cmd.
    /// </summary>
    private async Task ExecuteInCmd(string command)
    {
      ProcessStartInfo psi = new ProcessStartInfo
      {
        FileName = "cmd.exe",
        Arguments = $"/C {command}",
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        CreateNoWindow = true
      };

      try
      {
        using (Process process = new Process { StartInfo = psi })
        {
          process.Start();
          string output = await process.StandardOutput.ReadToEndAsync();
          string error = await process.StandardError.ReadToEndAsync();

          if (!string.IsNullOrWhiteSpace(output))
            Console.WriteLine(output);
          if (!string.IsNullOrWhiteSpace(error))
            Console.WriteLine($"Ошибка: {error}");
        }
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Ошибка выполнения команды: {ex.Message}");
      }
    }

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
  /// Перехватывает весь вывод в консоли и записывает его в StringBuilder.
  /// </summary>
  public class ConsoleWriter : TextWriter
  {
    private readonly TextWriter _originalConsole;
    private readonly StringBuilder _consoleLog;

    public ConsoleWriter(TextWriter originalConsole, StringBuilder consoleLog)
    {
      _originalConsole = originalConsole;
      _consoleLog = consoleLog;
    }

    public override void Write(char value)
    {
      _originalConsole.Write(value);
      _consoleLog.Append(value);
    }

    public override void Write(string value)
    {
      _originalConsole.Write(value);
      _consoleLog.Append(value);
    }

    public override Encoding Encoding => Encoding.UTF8;
  }
}
