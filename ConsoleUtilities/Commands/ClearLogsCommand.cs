using ConsoleUtilities.Core;
using ConsoleUtilities.Models;

namespace ConsoleUtilities.Commands
{
  /// <summary>
  /// Удаляет все лог-файлы из папки console_logs.
  /// </summary>
  public class ClearLogsCommand : ICommand
  {
    /// <inheritdoc />
    public string Name => "clearlogs";

    /// <inheritdoc />
    public Task ExecuteAsync(string[] args, CommandContext context)
    {
      string logDir = Path.Combine(AppContext.BaseDirectory, "console_logs");

      if (!Directory.Exists(logDir))
      {
        context.Console.WriteLine("Папка с логами не найдена.");
        return Task.CompletedTask;
      }

      var files = Directory.GetFiles(logDir, "*.log");
      if (files.Length == 0)
      {
        context.Console.WriteLine("Нет логов для удаления.");
        return Task.CompletedTask;
      }

      int deleted = 0;
      foreach (var file in files)
      {
        try
        {
          File.Delete(file);
          deleted++;
        }
        catch (Exception ex)
        {
          context.Console.WriteLine($"Ошибка при удалении: {Path.GetFileName(file)} — {ex.Message}");
        }
      }

      context.Console.WriteLine($"Удалено файлов: {deleted}");
      return Task.CompletedTask;
    }
  }
}
