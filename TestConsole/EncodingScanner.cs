using System.Text;

namespace TestConsole;

internal static class EncodingScanner
{
  private static readonly UTF8Encoding StrictUtf8 = new(false, true);
  private static readonly Encoding Windows1251Encoding = CreateStrictEncoding(1251);
  private static readonly Encoding Cp866Encoding = CreateStrictEncoding(866);

  private static readonly string[] SourceFileExtensions =
  {
    ".cs",
    ".xaml",
    ".resx",
    ".xml",
    ".json",
    ".config"
  };

  private static readonly string[] MojibakeMarkers =
  {
    "Рџ",
    "РЎ",
    "Р’",
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
    "Ñ",
    "╨",
    "╤"
  };

  public static void Run()
  {
    Console.OutputEncoding = Encoding.UTF8;

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
      .EnumerateFiles(solutionDirectory, "*.*", SearchOption.AllDirectories)
      .Where(IsSupportedSourceFile)
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
    string text;

    try
    {
      byte[] bytes = File.ReadAllBytes(filePath);
      text = StrictUtf8.GetString(bytes);
    }
    catch (DecoderFallbackException ex)
    {
      return new EncodingIssueFile(
        filePath,
        Path.GetRelativePath(solutionDirectory, filePath),
        new[]
        {
          new EncodingIssueLine(
            0,
            $"Файл не является корректным UTF-8: {ex.Message}",
            null,
            EncodingProblemType.InvalidUtf8)
        });
    }

    string[] lines = SplitLines(text);
    var issues = new List<EncodingIssueLine>();

    for (int i = 0; i < lines.Length; i++)
    {
      EncodingIssueLine? issue = DetectProblem(
        filePath,
        i + 1,
        lines[i]);

      if (issue is not null)
      {
        issues.Add(issue);
      }
    }

    if (issues.Count == 0)
      return null;

    return new EncodingIssueFile(
      filePath,
      Path.GetRelativePath(solutionDirectory, filePath),
      issues);
  }

  private static EncodingIssueLine? DetectProblem(
    string filePath,
    int lineNumber,
    string line)
  {
    if (string.IsNullOrWhiteSpace(line))
      return null;

    if (line.Contains('�') && !IsIntentionalReplacementCharCheck(filePath, line))
    {
      return new EncodingIssueLine(
        lineNumber,
        line.Trim(),
        null,
        EncodingProblemType.Destroyed);
    }

    EncodingRecovery? recovery = TryRecoverMojibake(line);
    if (recovery is null)
      return null;

    return new EncodingIssueLine(
      lineNumber,
      line.Trim(),
      recovery.Value.Text.Trim(),
      EncodingProblemType.Recoverable);
  }

  private static bool IsIntentionalReplacementCharCheck(
    string filePath,
    string line)
  {
    return Path.GetFileName(filePath).Equals(
             "EncodingScanner.cs",
             StringComparison.OrdinalIgnoreCase)
           && line.Contains("Contains('�')", StringComparison.Ordinal);
  }

  private static EncodingRecovery? TryRecoverMojibake(string line)
  {
    int originalMojibakeScore = CalculateMojibakeScore(line);
    if (originalMojibakeScore < 3)
      return null;

    EncodingRecovery? bestRecovery = null;

    foreach (Encoding sourceEncoding in new[] { Windows1251Encoding, Cp866Encoding })
    {
      EncodingRecovery? recovery = TryRecoverMojibake(
        line,
        sourceEncoding,
        originalMojibakeScore);

      if (recovery is null)
        continue;

      if (bestRecovery is null || recovery.Value.Score > bestRecovery.Value.Score)
      {
        bestRecovery = recovery;
      }
    }

    return bestRecovery;
  }

  private static EncodingRecovery? TryRecoverMojibake(
    string line,
    Encoding sourceEncoding,
    int originalMojibakeScore)
  {
    string restored;

    try
    {
      byte[] bytes = sourceEncoding.GetBytes(line);
      restored = StrictUtf8.GetString(bytes);
    }
    catch (Exception ex) when (ex is EncoderFallbackException or DecoderFallbackException)
    {
      return null;
    }

    int restoredMojibakeScore = CalculateMojibakeScore(restored);
    int restoredCyrillicScore = CountCyrillicLetters(restored);

    if (restoredMojibakeScore > originalMojibakeScore / 2)
      return null;

    if (restoredCyrillicScore < 3)
      return null;

    int score = (originalMojibakeScore - restoredMojibakeScore) + restoredCyrillicScore;
    return new EncodingRecovery(restored, score);
  }

  private static int CalculateMojibakeScore(string line)
  {
    int score = 0;

    foreach (string marker in MojibakeMarkers)
    {
      score += CountOccurrences(line, marker);
    }

    return score;
  }

  private static int CountCyrillicLetters(string line)
  {
    int count = 0;

    foreach (char character in line)
    {
      if (character >= '\u0400' && character <= '\u04FF')
      {
        count++;
      }
    }

    return count;
  }

  private static int CountOccurrences(string source, string value)
  {
    int count = 0;
    int index = 0;

    while (index < source.Length)
    {
      index = source.IndexOf(value, index, StringComparison.Ordinal);

      if (index < 0)
        break;

      count++;
      index += value.Length;
    }

    return count;
  }

  private static string[] SplitLines(string text)
  {
    return text
      .Replace("\r\n", "\n", StringComparison.Ordinal)
      .Replace('\r', '\n')
      .Split('\n');
  }

  private static bool IsSupportedSourceFile(string file)
  {
    string normalized = file.Replace('\\', '/');

    if (normalized.Contains("/bin/", StringComparison.OrdinalIgnoreCase)
        || normalized.Contains("/obj/", StringComparison.OrdinalIgnoreCase)
        || normalized.Contains("/.git/", StringComparison.OrdinalIgnoreCase)
        || normalized.Contains("/.vs/", StringComparison.OrdinalIgnoreCase)
        || normalized.Contains("/packages/", StringComparison.OrdinalIgnoreCase))
    {
      return false;
    }

    string extension = Path.GetExtension(file);

    return SourceFileExtensions.Contains(
      extension,
      StringComparer.OrdinalIgnoreCase);
  }

  private static void PrintResults(IReadOnlyCollection<EncodingIssueFile> results)
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
          EncodingProblemType.InvalidUtf8 => ConsoleColor.Red,
          EncodingProblemType.Destroyed => ConsoleColor.Red,
          EncodingProblemType.Recoverable => ConsoleColor.Yellow,
          _ => Console.ForegroundColor
        };

        Console.WriteLine(
          $"  {issue.LineNumber}: [{issue.ProblemType}] {TrimPreview(issue.Content)}");

        Console.ResetColor();

        if (!string.IsNullOrWhiteSpace(issue.RecoveredContent))
        {
          Console.ForegroundColor = ConsoleColor.DarkGray;
          Console.WriteLine($"      -> {TrimPreview(issue.RecoveredContent)}");
          Console.ResetColor();
        }
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
    string? solutionPath = FindSolutionPath(new DirectoryInfo(Directory.GetCurrentDirectory()));
    if (solutionPath is not null)
      return solutionPath;

    return FindSolutionPath(new DirectoryInfo(AppContext.BaseDirectory));
  }

  private static string? FindSolutionPath(DirectoryInfo? current)
  {
    while (current is not null)
    {
      string candidate = Path.Combine(current.FullName, "AskMkiM.sln");

      if (File.Exists(candidate))
        return candidate;

      current = current.Parent;
    }

    return null;
  }

  private static Encoding CreateStrictEncoding(int codePage)
  {
    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    return Encoding.GetEncoding(
      codePage,
      EncoderFallback.ExceptionFallback,
      DecoderFallback.ExceptionFallback);
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
    string? RecoveredContent,
    EncodingProblemType ProblemType);

  private enum EncodingProblemType
  {
    InvalidUtf8,
    Recoverable,
    Destroyed
  }

  private readonly record struct EncodingRecovery(
    string Text,
    int Score);
}
