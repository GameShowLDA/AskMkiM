using ConsoleUtilities.Core;
using ConsoleUtilities.Models;
using System.IO;
using System.Text;

namespace ConsoleUtilities.Commands
{
  /// <summary>
  /// Сохраняет содержимое консоли в файл лога.
  /// </summary>
  public class SaveLogCommand : ICommand
  {
    /// <inheritdoc />
    public string Name => "save";

    /// <inheritdoc />
    public async Task ExecuteAsync(string[] args, CommandContext context)
    {
      if (context.ConsoleLogBuffer == null || context.ConsoleLogBuffer.Length == 0)
      {
        context.Console.WriteLine("Консоль пуста, нечего сохранять.");
        return;
      }

      string logsDir = Path.Combine(AppContext.BaseDirectory, "console_logs");
      Directory.CreateDirectory(logsDir);

      string fileName = $"console_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.log";
      string filePath = Path.Combine(logsDir, fileName);

      try
      {
        await File.WriteAllTextAsync(filePath, context.ConsoleLogBuffer.ToString(), Encoding.UTF8);
        context.Console.WriteLine($"Лог консоли сохранён: {filePath}");
      }
      catch (Exception ex)
      {
        context.Console.WriteLine($"Ошибка при сохранении: {ex.Message}");
      }
    }
  }
}
