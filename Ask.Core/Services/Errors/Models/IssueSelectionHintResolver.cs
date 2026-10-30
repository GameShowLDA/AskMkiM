using System.Text.RegularExpressions;

namespace Ask.Core.Services.Errors.Models
{
  public readonly record struct IssueSelectionHint(int StartIndex, int Length);

  public static class IssueSelectionHintResolver
  {
    private static readonly Regex QuotedValueRegex = new(@"'([^']+)'|""([^""]+)""", RegexOptions.Compiled);
    private static readonly Regex DescriptionTokenRegex = new(
      @"[^\s,;:()\[\]{}]+",
      RegexOptions.Compiled);
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

      // Syntax errors are often reported with a human readable description
      // that contains the offending point, range, key or parameter, but no
      // dedicated source span. Recover that token from the description so
      // the editor can underline the actual fragment instead of the command.
      if (!issue.IsWarning && TryResolveSyntaxToken(issue, lineText, out hint))
        return true;

      hint = default;
      return false;
    }

    private static bool TryResolveSyntaxToken(IDisplayIssue issue, string lineText, out IssueSelectionHint hint)
    {
      var commandTokens = (issue.Command ?? string.Empty)
        .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

      var candidates = DescriptionTokenRegex.Matches(issue.Description ?? string.Empty)
        .Select(match => TrimCandidate(match.Value))
        .Where(candidate => candidate.Length >= 2 || IsUppercaseSingleToken(candidate))
        .Where(candidate => !commandTokens.Contains(candidate))
        .Where(candidate => !IsCommonDescriptionWord(candidate))
        .Where(candidate => CandidateScore(candidate) > 0)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .Select(candidate => (Candidate: candidate, Score: CandidateScore(candidate)))
        .OrderByDescending(item => item.Score)
        .ThenByDescending(item => item.Candidate.Length)
        .Select(item => item.Candidate);

      foreach (var candidate in candidates)
      {
        if (TryFindCandidate(lineText, candidate, out hint))
          return true;
      }

      hint = default;
      return false;
    }

    private static int CandidateScore(string candidate)
    {
      int score = 0;
      if (candidate.Any(char.IsDigit)) score += 100;
      if (candidate.Any(ch => !char.IsLetterOrDigit(ch) && ch != '_')) score += 50;
      if (candidate.All(ch => !char.IsLetter(ch) || char.IsUpper(ch))) score += 20;
      return score;
    }

    private static bool IsCommonDescriptionWord(string candidate)
    {
      return candidate.Equals("команда", StringComparison.OrdinalIgnoreCase)
        || candidate.Equals("параметр", StringComparison.OrdinalIgnoreCase)
        || candidate.Equals("параметры", StringComparison.OrdinalIgnoreCase)
        || candidate.Equals("выражение", StringComparison.OrdinalIgnoreCase)
        || candidate.Equals("точка", StringComparison.OrdinalIgnoreCase)
        || candidate.Equals("точки", StringComparison.OrdinalIgnoreCase)
        || candidate.Equals("цепь", StringComparison.OrdinalIgnoreCase)
        || candidate.Equals("цепи", StringComparison.OrdinalIgnoreCase)
        || candidate.Equals("диапазон", StringComparison.OrdinalIgnoreCase)
        || candidate.Equals("ошибка", StringComparison.OrdinalIgnoreCase)
        || candidate.Equals("недопустимая", StringComparison.OrdinalIgnoreCase)
        || candidate.Equals("недопустимые", StringComparison.OrdinalIgnoreCase)
        || candidate.Equals("неверно", StringComparison.OrdinalIgnoreCase)
        || candidate.Equals("указаны", StringComparison.OrdinalIgnoreCase)
        || candidate.Equals("указана", StringComparison.OrdinalIgnoreCase)
        || candidate.Equals("указано", StringComparison.OrdinalIgnoreCase)
        || candidate.Equals("найден", StringComparison.OrdinalIgnoreCase)
        || candidate.Equals("найдена", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsUppercaseSingleToken(string candidate)
    {
      return candidate.Length == 1 && char.IsLetter(candidate[0]) && char.IsUpper(candidate[0]);
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
      if (index >= 0 && (candidate.Length != 1 || IsStandaloneToken(lineText, index, candidate.Length)))
      {
        hint = new IssueSelectionHint(index, candidate.Length);
        return true;
      }

      // Parsers frequently normalize whitespace while the editor preserves
      // the original spelling. Match the same candidate with flexible spaces
      // and return the span from the original line.
      if (candidate.Any(char.IsWhiteSpace))
      {
        var pattern = Regex.Escape(candidate).Replace(@"\ ", @"\s+");
        var match = Regex.Match(lineText, pattern, RegexOptions.IgnoreCase);
        if (match.Success)
        {
          hint = new IssueSelectionHint(match.Index, match.Length);
          return true;
        }
      }

      // Point parsers may remove spaces before putting the offending token
      // into the diagnostic ("sddbfgbК1/40"), while the editor still has
      // them ("sddb fgb К1/40"). Match that normalized token and return the
      // span from the original source.
      if (TryFindWhitespaceInsensitiveCandidate(lineText, candidate, out hint))
        return true;

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

    private static bool IsStandaloneToken(string lineText, int start, int length)
    {
      int end = start + length;
      bool startsToken = start == 0 || !char.IsLetterOrDigit(lineText[start - 1]);
      bool endsToken = end >= lineText.Length || !char.IsLetterOrDigit(lineText[end]);
      return startsToken && endsToken;
    }

    private static bool TryFindWhitespaceInsensitiveCandidate(
      string lineText,
      string candidate,
      out IssueSelectionHint hint)
    {
      var compactCandidate = string.Concat(candidate.Where(character => !char.IsWhiteSpace(character)));
      if (compactCandidate.Length < 2 ||
          compactCandidate.All(character => char.IsLetter(character) && char.IsUpper(character)))
      {
        hint = default;
        return false;
      }

      for (int start = 0; start < lineText.Length; start++)
      {
        int sourceIndex = start;
        int candidateIndex = 0;

        while (candidateIndex < compactCandidate.Length)
        {
          while (sourceIndex < lineText.Length && char.IsWhiteSpace(lineText[sourceIndex]))
            sourceIndex++;

          if (sourceIndex >= lineText.Length ||
              char.ToUpperInvariant(lineText[sourceIndex]) != char.ToUpperInvariant(compactCandidate[candidateIndex]))
            break;

          sourceIndex++;
          candidateIndex++;
        }

        if (candidateIndex == compactCandidate.Length)
        {
          hint = new IssueSelectionHint(start, sourceIndex - start);
          return true;
        }
      }

      hint = default;
      return false;
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
