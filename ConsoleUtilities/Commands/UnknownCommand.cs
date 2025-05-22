using System.Diagnostics;
using ConsoleUtilities.Core;
using ConsoleUtilities.Models;
using ConsoleUtilities.Services;

namespace ConsoleUtilities.Commands
{
  /// <summary>
  /// Команда-заглушка, перенаправляющая неизвестный ввод в системный cmd.
  /// </summary>
  public class UnknownCommand : ICommand
  {
    /// <inheritdoc />
    public string Name => _inputName;

    private readonly string _inputName;

    /// <summary>
    /// Создаёт экземпляр <see cref="UnknownCommand"/>.
    /// </summary>
    /// <param name="inputName">Введённая пользователем строка команды.</param>
    public UnknownCommand(string inputName)
    {
      _inputName = inputName;
    }

    /// <inheritdoc />
    public async Task ExecuteAsync(string[] args, CommandContext context)
    {
      string actualCommand = _inputName.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();

      if (!CmdCommandValidator.IsCmdCommand(actualCommand))
      {
        context.Console.WriteLine($"Команда \"{_inputName}\" не найдена. Введите 'help' для списка доступных команд.");
        return;
      }

      context.Console.WriteLine($"Выполняем \"{_inputName}\" через cmd...");

      var psi = new ProcessStartInfo
      {
        FileName = "cmd.exe",
        Arguments = $"/C {_inputName}",
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        CreateNoWindow = true
      };

      try
      {
        using var process = new Process { StartInfo = psi };
        process.Start();

        string output = await process.StandardOutput.ReadToEndAsync();
        string error = await process.StandardError.ReadToEndAsync();

        if (!string.IsNullOrWhiteSpace(output))
        {
          context.Console.WriteLine(output.Trim());
        }

        if (!string.IsNullOrWhiteSpace(error))
        {
          context.Console.WriteLine($"[Ошибка] {error.Trim()}");
        }
      }
      catch (Exception ex)
      {
        context.Console.WriteLine($"Ошибка выполнения команды через cmd: {ex.Message}");
      }
    }
  }
}
