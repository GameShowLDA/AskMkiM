using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace TestConsole;

internal static class EmptyLineScanner
{
  public static void Run()
  {
    Console.WriteLine();
    Console.WriteLine("=== Проверка лишних пустых строк ===");

    string? solutionPath = FindSolutionPath();
    if (solutionPath is null)
    {
      WriteError("Не удалось найти файл решения AskMkiM.sln.");
      return;
    }

    string solutionDirectory = Path.GetDirectoryName(solutionPath)
      ?? throw new InvalidOperationException("Не удалось определить директорию solution.");

    List<string> files = Directory
      .EnumerateFiles(solutionDirectory, "*.cs", SearchOption.AllDirectories)
      .Where(static file =>
      {
        string normalized = file.Replace('\\', '/');

        return !normalized.Contains("/bin/", StringComparison.OrdinalIgnoreCase)
               && !normalized.Contains("/obj/", StringComparison.OrdinalIgnoreCase);
      })
      .ToList();

    var results = new List<FileEmptyLineResult>();

    foreach (string file in files)
    {
      FileEmptyLineResult? result = ScanFile(solutionDirectory, file);

      if (result is not null)
      {
        results.Add(result);
      }
    }

    PrintResults(results);
    PromptFix(results);
  }

  private static FileEmptyLineResult? ScanFile(
    string solutionDirectory,
    string filePath)
  {
    string text = File.ReadAllText(filePath);

    SyntaxTree tree = CSharpSyntaxTree.ParseText(text);
    SourceText sourceText = tree.GetText();

    string[] lines = sourceText.Lines
      .Select(static line => line.ToString())
      .ToArray();

    var ranges = new List<EmptyLineRange>();

    int startLine = -1;
    int emptyCount = 0;

    for (int i = 0; i < lines.Length; i++)
    {
      bool isEmpty = string.IsNullOrWhiteSpace(lines[i]);

      if (isEmpty)
      {
        if (emptyCount == 0)
        {
          startLine = i + 1;
        }

        emptyCount++;
      }
      else
      {
        if (emptyCount > 1)
        {
          ranges.Add(new EmptyLineRange(
            startLine,
            i));
        }

        emptyCount = 0;
      }
    }

    if (emptyCount > 1)
    {
      ranges.Add(new EmptyLineRange(
        startLine,
        lines.Length));
    }

    if (ranges.Count == 0)
      return null;

    string relativePath = Path.GetRelativePath(solutionDirectory, filePath);

    return new FileEmptyLineResult(
      filePath,
      relativePath,
      ranges);
  }

  private static void PrintResults(
    IReadOnlyCollection<FileEmptyLineResult> results)
  {
    Console.WriteLine();
    Console.WriteLine($"Файлов с проблемами: {results.Count}");

    if (results.Count == 0)
    {
      WriteSuccess("Лишних пустых строк не найдено.");
      return;
    }

    foreach (FileEmptyLineResult result in results
               .OrderBy(static result => result.RelativePath, StringComparer.OrdinalIgnoreCase))
    {
      Console.WriteLine();

      Console.ForegroundColor = ConsoleColor.Cyan;
      Console.WriteLine(result.RelativePath);
      Console.ResetColor();

      for (int i = 0; i < result.Ranges.Count; i++)
      {
        EmptyLineRange range = result.Ranges[i];

        Console.WriteLine($"{i + 1}. {range.StartLine}-{range.EndLine}");
      }
    }
  }

  private static void PromptFix(
    IReadOnlyCollection<FileEmptyLineResult> results)
  {
    if (results.Count == 0)
      return;

    Console.WriteLine();

    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("Будут удалены повторяющиеся пустые строки.");
    Console.WriteLine("Между блоками будет оставлена только одна пустая строка.");
    Console.ResetColor();

    Console.Write("Исправить автоматически? [y/N]: ");

    string? answer = Console.ReadLine();

    if (!IsYes(answer))
    {
      Console.WriteLine("Автоисправление отменено.");
      return;
    }

    ApplyFixes(results);
  }

  private static void ApplyFixes(
    IReadOnlyCollection<FileEmptyLineResult> results)
  {
    int fixedFiles = 0;

    foreach (FileEmptyLineResult result in results)
    {
      try
      {
        string source = File.ReadAllText(result.AbsolutePath);
        string updated = NormalizeEmptyLines(source);

        if (string.Equals(source, updated, StringComparison.Ordinal))
          continue;

        File.WriteAllText(result.AbsolutePath, updated);

        fixedFiles++;

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"Исправлен: {result.RelativePath}");
        Console.ResetColor();
      }
      catch (Exception ex)
      {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"Ошибка: {result.RelativePath}");
        Console.WriteLine(ex.Message);
        Console.ResetColor();
      }
    }

    Console.WriteLine();
    Console.WriteLine($"Исправлено файлов: {fixedFiles}");
  }

  private static string NormalizeEmptyLines(string source)
  {
    string newLine = DetectNewLine(source);

    string normalized = source
      .Replace("\r\n", "\n", StringComparison.Ordinal)
      .Replace('\r', '\n');

    string[] lines = normalized.Split('\n');

    var result = new List<string>(lines.Length);

    bool previousWasEmpty = false;

    foreach (string line in lines)
    {
      bool isEmpty = string.IsNullOrWhiteSpace(line);

      if (isEmpty)
      {
        if (previousWasEmpty)
          continue;

        previousWasEmpty = true;
      }
      else
      {
        previousWasEmpty = false;
      }

      result.Add(line);
    }

    return string.Join(newLine, result);
  }

  private static bool IsYes(string? answer)
  {
    if (string.IsNullOrWhiteSpace(answer))
      return false;

    return answer.Trim().ToLowerInvariant() switch
    {
      "y" => true,
      "yes" => true,
      "д" => true,
      "да" => true,
      _ => false
    };
  }

  private static string DetectNewLine(string source)
  {
    return source.Contains("\r\n", StringComparison.Ordinal)
      ? "\r\n"
      : "\n";
  }

  private static string? FindSolutionPath()
  {
    var current = new DirectoryInfo(AppContext.BaseDirectory);

    while (current is not null)
    {
      string candidate = Path.Combine(current.FullName, "AskMkiM.sln");

      if (File.Exists(candidate))
        return candidate;

      current = current.Parent;
    }

    return null;
  }

  private static void WriteError(string message)
  {
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine(message);
    Console.ResetColor();
  }

  private static void WriteSuccess(string message)
  {
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine(message);
    Console.ResetColor();
  }

  private sealed record FileEmptyLineResult(
    string AbsolutePath,
    string RelativePath,
    IReadOnlyList<EmptyLineRange> Ranges);

  private sealed record EmptyLineRange(
    int StartLine,
    int EndLine);
}