using System.Text.RegularExpressions;

namespace Ask.Core.Services.Errors.Models
{
  public readonly record struct IssueSelectionHint(int StartIndex, int Length);

  public static class IssueSelectionHintResolver
  {
    private static readonly Regex QuotedValueRegex = new(@"'([^']+)'|""([^""]+)""", RegexOptions.Compiled);
    private static readonly Regex VoltageValueRegex = new(
      @"(?<![\d.,])\d+(?:[.,]\d+)?\s*(?:\u043A\u0412|\u041A\u0412|\u043C\u0412|\u041C\u0412|\u0412)\b",
      RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly string[] Prefixes =
    {
      "Не удалось распознать параметры:",
      "Обнаружены нераспознанные параметры:",
      "Не удалось распознать выражение:",
      "Не удалось разобрать параметр:",
      "Замкнутая пара точек:",
      "Замыкание точек:",
      "Замкнутая пара цепей:",
      "Замыкание цепей:",
      "Левая или правая часть выражения пуста:",
      "Обнаружены недопустимые символы в выражении:",
      "Обнаружена недопустимая последовательность -"
    };

    public static bool TryResolve(IDisplayIssue issue, string lineText, out IssueSelectionHint hint)
    {
      foreach (var candidate in GetCandidates(issue))
      {
        if (TryFindCandidate(lineText, candidate, out hint))
          return true;
      }

      if (TryResolveVoltageIssue(issue, lineText, out hint))
        return true;

      hint = default;
      return false;
    }

    private static bool TryResolveVoltageIssue(IDisplayIssue issue, string lineText, out IssueSelectionHint hint)
    {
      var code = issue.CodeString ?? string.Empty;
      if (!code.Contains("Voltage", StringComparison.OrdinalIgnoreCase))
      {
        hint = default;
        return false;
      }

      var matches = VoltageValueRegex.Matches(lineText);
      if (matches.Count == 0)
      {
        hint = default;
        return false;
      }

      var description = issue.Description ?? string.Empty;
      var isUpperBound = ContainsAny(description, "\u0412\u0435\u0440\u0445", "\u0431\u043E\u043B\u044C\u0448", "\u043F\u0440\u0435\u0432\u044B\u0448");
      var isLowerBound = ContainsAny(description, "\u041D\u0438\u0436", "\u043C\u0435\u043D\u044C\u0448");
      var match = isUpperBound && !isLowerBound ? matches[^1] : matches[0];

      hint = new IssueSelectionHint(match.Index, match.Length);
      return true;
    }

    private static bool ContainsAny(string value, params string[] fragments)
    {
      return fragments.Any(fragment => value.Contains(fragment, StringComparison.OrdinalIgnoreCase));
    }

    private static IEnumerable<string> GetCandidates(IDisplayIssue issue)
    {
      foreach (var candidate in GetQuotedCandidates(issue.Description))
        yield return candidate;

      foreach (var prefix in Prefixes)
      {
        var candidate = GetTextAfterPrefix(issue.Description, prefix);
        if (!string.IsNullOrWhiteSpace(candidate))
          yield return candidate;
      }

      foreach (var candidate in GetKeywordCandidates(issue.Description))
        yield return candidate;

      if (!string.IsNullOrWhiteSpace(issue.MeasureResult))
        yield return issue.MeasureResult;
    }

    private static IEnumerable<string> GetQuotedCandidates(string text)
    {
      foreach (Match match in QuotedValueRegex.Matches(text ?? string.Empty))
      {
        var value = match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value;
        if (!string.IsNullOrWhiteSpace(value))
          yield return value;
      }
    }

    private static string? GetTextAfterPrefix(string text, string prefix)
    {
      if (string.IsNullOrWhiteSpace(text))
        return null;

      var index = text.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
      if (index < 0)
        return null;

      return TrimCandidate(text[(index + prefix.Length)..]);
    }

    private static IEnumerable<string> GetKeywordCandidates(string text)
    {
      if (string.IsNullOrWhiteSpace(text))
        yield break;

      foreach (var prefix in new[] { "Ошибка при проверке цепи ", "Разрыв в цепи ", "Замкнутая цепь ", "Замыкание в цепи ", "Замыкание цепи " })
      {
        var candidate = GetTextAfterPrefix(text, prefix);
        if (!string.IsNullOrWhiteSpace(candidate))
          yield return TrimBefore(candidate, " при ");
      }

      foreach (var prefix in new[] { "недопустимо использование ключа ", "найден дублирующийся ключ:", "Неизвестная команда " })
      {
        var candidate = GetTextAfterPrefix(text, prefix);
        if (!string.IsNullOrWhiteSpace(candidate))
          yield return candidate;
      }
    }

    private static bool TryFindCandidate(string lineText, string candidate, out IssueSelectionHint hint)
    {
      candidate = TrimCandidate(candidate);
      if (string.IsNullOrWhiteSpace(candidate))
      {
        hint = default;
        return false;
      }

      var index = lineText.IndexOf(candidate, StringComparison.OrdinalIgnoreCase);
      if (index >= 0)
      {
        hint = new IssueSelectionHint(index, candidate.Length);
        return true;
      }

      var parts = SplitCandidate(candidate).ToArray();
      if (parts.Length <= 1)
      {
        hint = default;
        return false;
      }

      var ranges = new List<IssueSelectionHint>();
      foreach (var part in parts)
      {
        var partIndex = lineText.IndexOf(part, StringComparison.OrdinalIgnoreCase);
        if (partIndex < 0)
        {
          hint = default;
          return false;
        }

        ranges.Add(new IssueSelectionHint(partIndex, part.Length));
      }

      var start = ranges.Min(range => range.StartIndex);
      var end = ranges.Max(range => range.StartIndex + range.Length);
      hint = new IssueSelectionHint(start, end - start);
      return true;
    }

    private static IEnumerable<string> SplitCandidate(string candidate)
    {
      return candidate
        .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .SelectMany(part => part.Split(new[] { " и " }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        .Select(TrimCandidate)
        .Where(part => part.Length > 0);
    }

    private static string TrimBefore(string value, string marker)
    {
      var index = value.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
      return index >= 0 ? TrimCandidate(value[..index]) : TrimCandidate(value);
    }

    private static string TrimCandidate(string value)
    {
      return (value ?? string.Empty).Trim().Trim('.', ',', ';', ':', ' ', '"', '\'');
    }
  }
}
