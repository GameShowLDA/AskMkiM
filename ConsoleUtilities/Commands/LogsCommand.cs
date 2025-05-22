using ConsoleUtilities.Core;
using ConsoleUtilities.Models;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace ConsoleUtilities.Commands
{
  public class LogsCommand : ICommand
  {
    public string Name => "logs";

    public async Task ExecuteAsync(string[] args, CommandContext context)
    {
      context.Console.WriteLine("Выберите источник логов:");
      context.Console.WriteLine("1. Текущие логи программы (NLog)");
      context.Console.WriteLine("2. Сохранённые лог-файлы (save)");
      context.Console.WriteLine("0. Выход");

      context.Console.Write("Ваш выбор: ");
      string input = Console.ReadLine();

      if (!int.TryParse(input, out int mode) || mode < 0 || mode > 2)
      {
        context.Console.WriteLine("Неверный выбор.");
        return;
      }

      if (mode == 0) return;

      string logDir = mode switch
      {
        1 => Path.Combine(AppContext.BaseDirectory, "logs"),
        2 => Path.Combine(AppContext.BaseDirectory, "console_logs"),
        _ => throw new InvalidOperationException()
      };

      if (!Directory.Exists(logDir))
      {
        context.Console.WriteLine("Папка логов не найдена.");
        return;
      }

      var files = Directory.GetFiles(logDir, "*.log")
                           .OrderByDescending(File.GetCreationTime)
                           .ToList();

      if (files.Count == 0)
      {
        context.Console.WriteLine("Нет логов для отображения.");
        return;
      }

      context.Console.WriteLine("\nСписок логов:");
      for (int i = 0; i < files.Count; i++)
      {
        context.Console.WriteLine($"{i + 1}. {Path.GetFileName(files[i])}");
      }

      context.Console.Write("Введите номер файла для просмотра или 0 для отмены: ");
      string choice = Console.ReadLine();

      if (!int.TryParse(choice, out int index) || index < 0 || index > files.Count)
      {
        context.Console.WriteLine("Неверный выбор.");
        return;
      }

      if (index == 0) return;

      string filePath = files[index - 1];

      // Фильтрация строк
      List<string> filteredLines;
      try
      {
        filteredLines = new List<string>();

        using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        using (var sr = new StreamReader(fs, Encoding.UTF8))
        {
          while (!sr.EndOfStream)
          {
            var line = await sr.ReadLineAsync();
            if (line.Contains("ERROR") || line.Contains("WARN"))
            {
              filteredLines.Add(line);
            }
          }
        }
      }
      catch (Exception ex)
      {
        context.Console.WriteLine($"Ошибка при чтении файла: {ex.Message}");
        return;
      }

      if (filteredLines.Count == 0)
      {
        context.Console.WriteLine("Ошибок и предупреждений не найдено.");
        return;
      }

      // Создание временного файла без BOM
      string tempPath = Path.Combine(Path.GetTempPath(), $"log_filtered_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
      try
      {
        File.WriteAllText(tempPath, string.Join(Environment.NewLine, filteredLines), new UTF8Encoding(false));
      }
      catch (Exception ex)
      {
        context.Console.WriteLine($"Ошибка при создании временного файла: {ex.Message}");
        return;
      }

      string cmdCommand = $"chcp 65001 >nul & more \"{tempPath}\" & pause";

      try
      {
        Process.Start(new ProcessStartInfo
        {
          FileName = "cmd.exe",
          Arguments = $"/C {cmdCommand}",
          UseShellExecute = true
        });

        context.Console.WriteLine("Открыт файл с фильтрованными логами в отдельной консоли.");
      }
      catch (Exception ex)
      {
        context.Console.WriteLine($"Не удалось запустить консоль: {ex.Message}");
      }
    }
  }
}
