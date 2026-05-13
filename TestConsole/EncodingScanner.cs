using System.Text.RegularExpressions;

namespace TestConsole;

internal static class EncodingScanner
{
  public static void Run()
  {
    Console.WriteLine();
    Console.WriteLine("=== Проверка сломанных кодировок ===");

    string? solutionPath = FindSolutionPath();
    if (solutionPath is null)
    {
      WriteError("Не удалось найти AskMkiM.sln.");
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

    var results = new List<EncodingIssueFile>();

    foreach (string file in files)
    {
      EncodingIssueFile? result = ScanFile(solutionDirectory, file);

      if (result is not null)
      {
        results.Add(result);
      }
    }

    PrintResults(results);
  }

  private static EncodingIssueFile? ScanFile(
    string solutionDirectory,
    string filePath)
  {
    string[] lines = File.ReadAllLines(filePath);

    var issues = new List<EncodingIssueLine>();

    for (int i = 0; i < lines.Length; i++)
    {
      string line = lines[i];

      EncodingProblemType? problemType = DetectProblemType(line);
      if (problemType is null)
        continue;

      issues.Add(new EncodingIssueLine(
        i + 1,
        line.Trim(),
        problemType.Value));
    }

    if (issues.Count == 0)
      return null;

    return new EncodingIssueFile(
      filePath,
      Path.GetRelativePath(solutionDirectory, filePath),
      issues);
  }

  private static EncodingProblemType? DetectProblemType(string line)
  {
    if (string.IsNullOrWhiteSpace(line))
      return null;

    if (line.Contains('�'))
      return EncodingProblemType.Destroyed;

    string[] mojibakeMarkers =
    {
      "Рџ",
      "РЎ",
      "Р°",
      "Рµ",
      "РЅ",
      "Рї",
      "Рч",
      "С‚",
      "СЏ",
      "С…",
      "Сѓ",
      "Ð",
      "Ñ"
    };

    int count = 0;

    foreach (string marker in mojibakeMarkers)
    {
      count += CountOccurrences(line, marker);
    }

    if (count >= 3)
      return EncodingProblemType.Recoverable;

    return null;
  }

  private static int CountOccurrences(
    string source,
    string value)
  {
    if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(value))
      return 0;

    int count = 0;
    int index = 0;

    while (true)
    {
      index = source.IndexOf(value, index, StringComparison.Ordinal);

      if (index < 0)
        break;

      count++;
      index += value.Length;
    }

    return count;
  }

  private static void PrintResults(
    IReadOnlyCollection<EncodingIssueFile> results)
  {
    Console.WriteLine();
    Console.WriteLine($"Файлов с проблемами кодировки: {results.Count}");

    if (results.Count == 0)
    {
      WriteSuccess("Проблем с кодировкой не найдено.");
      return;
    }

    foreach (EncodingIssueFile file in results
               .OrderBy(static file => file.RelativePath, StringComparer.OrdinalIgnoreCase))
    {
      Console.WriteLine();

      Console.ForegroundColor = ConsoleColor.Cyan;
      Console.WriteLine(file.RelativePath);
      Console.ResetColor();

      foreach (EncodingIssueLine issue in file.Issues)
      {
        Console.ForegroundColor = issue.ProblemType switch
        {
          EncodingProblemType.Destroyed => ConsoleColor.Red,
          EncodingProblemType.Recoverable => ConsoleColor.Yellow,
          _ => Console.ForegroundColor
        };

        Console.WriteLine(
          $"  {issue.LineNumber}: [{issue.ProblemType}] {TrimPreview(issue.Content)}");

        Console.ResetColor();
      }
    }
  }

  private static string TrimPreview(string text)
  {
    const int maxLength = 140;

    if (text.Length <= maxLength)
      return text;

    return text[..maxLength] + "...";
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

  private sealed record EncodingIssueFile(
    string AbsolutePath,
    string RelativePath,
    IReadOnlyList<EncodingIssueLine> Issues);

  private sealed record EncodingIssueLine(
    int LineNumber,
    string Content,
    EncodingProblemType ProblemType);

  private enum EncodingProblemType
  {
    Recoverable,
    Destroyed
  }
}