using Ask.Core.Services.Errors.Models;
using Ask.Core.Shared.DTO.Executor;
using Ask.Engine.ControlCommandAnalyser.RmTranslation.Parser;
using ICSharpCode.AvalonEdit.Document;
using System.Text.RegularExpressions;

namespace UI.Controls.TextEditorControl.Syntax
{
  /// <summary>
  /// Определяет участок исходного текста, который должен быть подчёркнут
  /// для ошибки или предупреждения командного анализатора.
  /// </summary>
  internal static class CommandIssueSpanResolver
  {
    private static readonly Regex ResistanceRegex = new Regex(
      @"(?<![\p{L}\p{N}])(?:[RР]\s*(?:<=|>=|<|>|=|≤|≥)\s*)?\d+(?:[.,]\d+)?\s*(?:Ом|кОм|МОм|ГОм)?(?:\s*(?:<=|>=|<|>|=|≤|≥)\s*(?:[RР]|\d+(?:[.,]\d+)?\s*(?:Ом|кОм|МОм|ГОм)?))?|(?<![\p{L}\p{N}])(?:Ом|кОм|МОм|ГОм)\s*(?:<=|>=|<|>|=|≤|≥)\s*\d+(?:[.,]\d+)?",
      RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly Regex VoltageRegex = new Regex(
      @"(?<![\p{L}\p{N}])\d+(?:[.,]\d+)?\s*(?:кВ|КВ|мВ|МВ|В|V|kV|mV)(?![\p{L}\p{N}])",
      RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly Regex TimeRegex = new Regex(
      @"(?<![\p{L}\p{N}])\d+(?:[.,]\d+)?\s*(?:мс|ms|с|c)(?![\p{L}\p{N}])|(?<![\p{L}\p{N}])(?:мс|ms|с|c)(?![\p{L}\p{N}])",
      RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly Regex AmperageRegex = new Regex(
      @"(?<![\p{L}\p{N}])\d+(?:[.,]\d+)?\s*(?:мкА|мА|А|uA|mkA|mA|A)(?![\p{L}\p{N}])",
      RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly Regex CapacityRegex = new Regex(
      @"(?<![\p{L}\p{N}])\d+(?:[.,]\d+)?\s*(?:пФ|нФ|мкФ|уФ|pF|nF|mkF|uF|Ф|F)(?![\p{L}\p{N}])",
      RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly Regex QuotedValueRegex = new Regex(
      @"'([^']+)'|""([^""]+)""",
      RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly string[] DirectPrefixes =
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

    private static readonly string[] KeywordPrefixes =
    {
      "Ошибка при проверке цепи ",
      "Разрыв в цепи ",
      "Замкнутая цепь ",
      "Замыкание в цепи ",
      "Замыкание цепи ",
      "недопустимо использование ключа ",
      "найден дублирующийся ключ:",
      "Неизвестная команда ",
      "Метка перехода ",
      "Стойка "
    };

    private static readonly ValuePattern[] ValuePatterns =
    {
      new ValuePattern(IssueKind.Resistance, ResistanceRegex),
      new ValuePattern(IssueKind.Voltage, VoltageRegex),
      new ValuePattern(IssueKind.Capacity, CapacityRegex),
      new ValuePattern(IssueKind.Amperage, AmperageRegex),
      new ValuePattern(IssueKind.Time, TimeRegex)
    };

    /// <summary>
    /// Пытается найти наиболее точный диапазон исходного документа для указанной диагностики.
    /// </summary>
    /// <param name="document">Документ AvalonEdit.</param>
    /// <param name="model">Модель команды, к которой относится диагностика.</param>
    /// <param name="issue">Ошибка или предупреждение командного анализатора.</param>
    /// <param name="modelEndLineNumber">Последняя строка команды в исходном документе.</param>
    /// <param name="commentSpans">Диапазоны комментариев, исключаемых из поиска.</param>
    /// <param name="span">Найденный диапазон исходного документа.</param>
    /// <returns>Значение true, если диапазон для подчёркивания найден.</returns>
    public static bool TryResolve(
      TextDocument document,
      BaseCommandModel model,
      IDisplayIssue issue,
      int modelEndLineNumber,
      IReadOnlyList<TextSpan> commentSpans,
      out CommandIssueSpan span)
    {
      int lineNumber = ResolveLineNumber(document, model, issue);
      var line = document.GetLineByNumber(lineNumber);
      string lineText = SyntaxCommentScanner.RemoveCommentsFromLine(
        document.GetText(line),
        line.Offset,
        commentSpans);
      bool hasBodyMap = CommandBodyMap.TryCreate(
        document,
        model,
        modelEndLineNumber,
        commentSpans,
        out var bodyMap);

      if (TryResolveLineHint(line, lineNumber, lineText, issue, out span))
        return true;

      if (hasBodyMap &&
          (TryResolveRmDiagnostic(model, issue, bodyMap, out span) ||
           TryResolveIssueText(bodyMap, issue, out span) ||
           TryResolveSemantic(bodyMap, issue, out span) ||
           TryResolveIssueSourceLine(bodyMap, issue, out span)))
      {
        return true;
      }

      if (TryResolveHeader(line, lineText, model, issue, out span))
        return true;

      return TryResolveNonWhiteSpaceLine(line, lineText, out span);
    }

    /// <summary>
    /// Проверяет точную подсказку выбора на исходной строке диагностики.
    /// </summary>
    private static bool TryResolveLineHint(
      DocumentLine line,
      int lineNumber,
      string lineText,
      IDisplayIssue issue,
      out CommandIssueSpan span)
    {
      if (IssueSelectionHintResolver.TryResolve(issue, lineText, out var hint) &&
          IsValidHint(lineText, hint))
      {
        span = new CommandIssueSpan(
          line.Offset + hint.StartIndex,
          hint.Length,
          lineNumber,
          hint.StartIndex + 1);
        return true;
      }

      span = default;
      return false;
    }

    /// <summary>
    /// Использует точные диапазоны парсера РМ, если диагностика относится к команде РМ.
    /// </summary>
    private static bool TryResolveRmDiagnostic(
      BaseCommandModel model,
      IDisplayIssue issue,
      CommandBodyMap bodyMap,
      out CommandIssueSpan span)
    {
      if (!model.IsCommandMnemonic("РМ") ||
          issue.CodeString?.StartsWith("Rm_", StringComparison.OrdinalIgnoreCase) != true)
      {
        span = default;
        return false;
      }

      var description = issue.Description ?? string.Empty;
      foreach (var diagnostic in RmParser.Parse(bodyMap.Text).Diagnostics)
      {
        if (!description.Contains($"{diagnostic.Code}: {diagnostic.Message}", StringComparison.OrdinalIgnoreCase) &&
            !description.Contains(diagnostic.Message, StringComparison.OrdinalIgnoreCase))
        {
          continue;
        }

        if (bodyMap.TryResolve(diagnostic.Span.Start, Math.Max(1, diagnostic.Span.Length), out span))
          return true;
      }

      return bodyMap.TryResolveFirstSegment(out span);
    }

    /// <summary>
    /// Ищет фрагменты из текста диагностики во всём теле команды.
    /// </summary>
    private static bool TryResolveIssueText(
      CommandBodyMap bodyMap,
      IDisplayIssue issue,
      out CommandIssueSpan span)
    {
      if (IssueSelectionHintResolver.TryResolve(issue, bodyMap.Text, out var hint) &&
          bodyMap.TryResolve(hint.StartIndex, hint.Length, out span))
      {
        return true;
      }

      foreach (var candidate in GetIssueCandidates(issue))
      {
        if (bodyMap.TryResolveText(candidate, out span) ||
            bodyMap.TryResolveCompactText(candidate, out span))
        {
          return true;
        }
      }

      span = default;
      return false;
    }

    /// <summary>
    /// Выбирает диапазон по смысловому типу диагностики:
    /// параметры, точки, сопротивление, напряжение, ёмкость, ток или время.
    /// </summary>
    private static bool TryResolveSemantic(
      CommandBodyMap bodyMap,
      IDisplayIssue issue,
      out CommandIssueSpan span)
    {
      var kind = GetIssueKind(issue);

      if (kind.HasFlag(IssueKind.CommaStar) && bodyMap.TryResolveText(",*", out span))
        return true;

      foreach (var pattern in ValuePatterns.Where(pattern => kind.HasFlag(pattern.Kind)))
      {
        if (TryResolveValue(bodyMap, pattern.Regex, issue, out span))
          return true;
      }

      if (kind.HasFlag(IssueKind.Points) && bodyMap.TryResolvePointRegion(out span))
        return true;

      if (kind.HasFlag(IssueKind.Parameters) && bodyMap.TryResolveRegion(bodyMap.ParameterText, out span))
        return true;

      if (kind.HasFlag(IssueKind.RequiredContent) && bodyMap.TryResolveFirstSegment(out span))
        return true;

      span = default;
      return false;
    }

    /// <summary>
    /// Ищет значение указанного типа в параметрической части команды.
    /// </summary>
    private static bool TryResolveValue(
      CommandBodyMap bodyMap,
      Regex regex,
      IDisplayIssue issue,
      out CommandIssueSpan span)
    {
      var matches = regex
        .Matches(bodyMap.ParameterText)
        .Cast<Match>()
        .Where(match => match.Length > 0)
        .ToArray();

      if (matches.Length == 0)
      {
        span = default;
        return false;
      }

      var match = SelectMatchByDescription(matches, issue.Description);
      return bodyMap.TryResolve(match.Index, match.Length, out span);
    }

    /// <summary>
    /// Возвращает диапазон строки, указанной самой диагностикой, если она попадает в тело команды.
    /// </summary>
    private static bool TryResolveIssueSourceLine(
      CommandBodyMap bodyMap,
      IDisplayIssue issue,
      out CommandIssueSpan span)
    {
      span = default;

      return issue.SourceLineNumber > 0
             && bodyMap.TryResolveLine(issue.SourceLineNumber, out span);
    }

    /// <summary>
    /// Возвращает диапазон заголовка команды, когда более точный участок не найден.
    /// </summary>
    private static bool TryResolveHeader(
      DocumentLine line,
      string lineText,
      BaseCommandModel model,
      IDisplayIssue issue,
      out CommandIssueSpan span)
    {
      foreach (var candidate in GetHeaderCandidates(model, issue))
      {
        int index = lineText.IndexOf(candidate, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
          continue;

        span = new CommandIssueSpan(
          line.Offset + index,
          candidate.Length,
          line.LineNumber,
          index + 1);
        return true;
      }

      span = default;
      return false;
    }

    /// <summary>
    /// Возвращает диапазон всей непустой части строки как последний запасной вариант.
    /// </summary>
    private static bool TryResolveNonWhiteSpaceLine(
      DocumentLine line,
      string lineText,
      out CommandIssueSpan span)
    {
      int start = 0;
      int end = lineText.Length;

      while (start < end && char.IsWhiteSpace(lineText[start]))
        start++;

      while (end > start && char.IsWhiteSpace(lineText[end - 1]))
        end--;

      if (end <= start)
      {
        span = default;
        return false;
      }

      span = new CommandIssueSpan(line.Offset + start, end - start, line.LineNumber, start + 1);
      return true;
    }

    /// <summary>
    /// Выбирает первое или последнее найденное значение по словам в описании диагностики.
    /// </summary>
    private static Match SelectMatchByDescription(IReadOnlyList<Match> matches, string? description)
    {
      description ??= string.Empty;
      bool upper = ContainsAny(description, "верх", "больш", "максим", "превыш");
      bool lower = ContainsAny(description, "ниж", "меньш", "миним");

      return upper && !lower ? matches[^1] : matches[0];
    }

    /// <summary>
    /// Извлекает из текста диагностики фрагменты, которые можно поискать в теле команды.
    /// </summary>
    private static IEnumerable<string> GetIssueCandidates(IDisplayIssue issue)
    {
      foreach (Match match in QuotedValueRegex.Matches(issue.Description ?? string.Empty))
      {
        var value = match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value;
        if (!string.IsNullOrWhiteSpace(value))
          yield return TrimCandidate(value);
      }

      foreach (var candidate in GetPrefixedCandidates(issue.Description, DirectPrefixes))
        yield return candidate;

      foreach (var candidate in GetPrefixedCandidates(issue.Description, KeywordPrefixes))
        yield return TrimBefore(candidate, " при ");

      foreach (var candidate in GetValueCandidates(issue))
        yield return candidate;

      if (!string.IsNullOrWhiteSpace(issue.MeasureResult))
        yield return TrimCandidate(issue.MeasureResult);
    }

    /// <summary>
    /// Извлекает фрагменты после известных текстовых префиксов диагностики.
    /// </summary>
    private static IEnumerable<string> GetPrefixedCandidates(string? text, IEnumerable<string> prefixes)
    {
      foreach (var prefix in prefixes)
      {
        var candidate = GetTextAfterPrefix(text, prefix);
        if (!string.IsNullOrWhiteSpace(candidate))
          yield return candidate;
      }
    }

    /// <summary>
    /// Извлекает из описания диагностики числовые значения подходящего типа.
    /// </summary>
    private static IEnumerable<string> GetValueCandidates(IDisplayIssue issue)
    {
      var description = issue.Description ?? string.Empty;
      var kind = GetIssueKind(issue);

      foreach (var pattern in ValuePatterns.Where(pattern => kind.HasFlag(pattern.Kind)))
      {
        foreach (Match match in pattern.Regex.Matches(description))
        {
          if (match.Length > 0)
            yield return TrimCandidate(match.Value);
        }
      }
    }

    /// <summary>
    /// Классифицирует диагностику по коду и пользовательскому описанию.
    /// </summary>
    private static IssueKind GetIssueKind(IDisplayIssue issue)
    {
      string code = issue.CodeString ?? string.Empty;
      string description = issue.Description ?? string.Empty;
      bool previousCommand = description.Contains("предшеств", StringComparison.OrdinalIgnoreCase) ||
                             code.Contains("PreviousCommandHasNoPoints", StringComparison.OrdinalIgnoreCase);

      var kind = IssueKind.None;
      AddIf(ref kind, IssueKind.CommaStar, code.Equals("Gen_CommaStar", StringComparison.OrdinalIgnoreCase) || description.Contains(",*", StringComparison.OrdinalIgnoreCase));
      AddIf(ref kind, IssueKind.Resistance, code.Contains("Resistance", StringComparison.OrdinalIgnoreCase) || description.Contains("сопротив", StringComparison.OrdinalIgnoreCase));
      AddIf(ref kind, IssueKind.Voltage, code.Contains("Voltage", StringComparison.OrdinalIgnoreCase) || ContainsAny(description, "напряж", "вольт"));
      AddIf(ref kind, IssueKind.Capacity, code.Contains("Capacity", StringComparison.OrdinalIgnoreCase) || ContainsAny(description, "емк", "ёмк"));
      AddIf(ref kind, IssueKind.Amperage, code.Contains("Amperage", StringComparison.OrdinalIgnoreCase) || ContainsAny(description, "сил", "ток"));
      AddIf(ref kind, IssueKind.Time, code.Contains("Time", StringComparison.OrdinalIgnoreCase) || description.Contains("врем", StringComparison.OrdinalIgnoreCase));
      AddIf(ref kind, IssueKind.Points, !previousCommand && (code.Contains("EmptyPoints", StringComparison.OrdinalIgnoreCase) || description.Contains("точк", StringComparison.OrdinalIgnoreCase)));
      AddIf(ref kind, IssueKind.Parameters, code.Contains("CannotParseParameters", StringComparison.OrdinalIgnoreCase) || code.Equals("Gen_UnrecognizedParameters", StringComparison.OrdinalIgnoreCase) || ContainsAny(description, "параметр", "границ"));
      AddIf(ref kind, IssueKind.RequiredContent, !previousCommand && IsRequiredContentIssue(code, description));
      return kind;
    }

    /// <summary>
    /// Определяет диагностики, связанные с отсутствующим содержимым команды.
    /// </summary>
    private static bool IsRequiredContentIssue(string code, string description)
    {
      return code.Contains("EmptyCommandBody", StringComparison.OrdinalIgnoreCase)
             || code.Contains("EmptyPoints", StringComparison.OrdinalIgnoreCase)
             || code.Contains("NoPointsBody", StringComparison.OrdinalIgnoreCase)
             || code.Contains("EmptyResistance", StringComparison.OrdinalIgnoreCase)
             || code.Contains("EmptyVoltage", StringComparison.OrdinalIgnoreCase)
             || code.Contains("EmptyAmperage", StringComparison.OrdinalIgnoreCase)
             || code.Contains("EmptyLowerCapacity", StringComparison.OrdinalIgnoreCase)
             || ContainsAny(description, "не указано", "не указаны", "не найден блок");
    }

    /// <summary>
    /// Добавляет флаг классификации, если условие выполнено.
    /// </summary>
    private static void AddIf(ref IssueKind kind, IssueKind value, bool condition)
    {
      if (condition)
        kind |= value;
    }

    /// <summary>
    /// Возвращает корректный номер строки диагностики в пределах документа.
    /// </summary>
    private static int ResolveLineNumber(TextDocument document, BaseCommandModel model, IDisplayIssue issue)
    {
      int lineNumber = issue.SourceLineNumber > 0 ? issue.SourceLineNumber : model.StartLineNumber;
      return Math.Clamp(lineNumber <= 0 ? 1 : lineNumber, 1, document.LineCount);
    }

    /// <summary>
    /// Возвращает варианты текста заголовка команды для запасного поиска.
    /// </summary>
    private static IEnumerable<string> GetHeaderCandidates(BaseCommandModel model, IDisplayIssue issue)
    {
      if (!string.IsNullOrWhiteSpace(issue.Command))
        yield return issue.Command;

      if (!string.IsNullOrWhiteSpace(model.CommandNumber) && !string.IsNullOrWhiteSpace(model.Mnemonic))
        yield return $"{model.CommandNumber} {model.Mnemonic}";

      if (!string.IsNullOrWhiteSpace(model.Mnemonic))
        yield return model.Mnemonic;

      if (!string.IsNullOrWhiteSpace(model.CommandNumber))
        yield return model.CommandNumber;
    }

    /// <summary>
    /// Проверяет, что подсказка выбора находится внутри указанной строки.
    /// </summary>
    private static bool IsValidHint(string lineText, IssueSelectionHint hint)
    {
      return hint.StartIndex >= 0
             && hint.Length > 0
             && hint.StartIndex + hint.Length <= lineText.Length;
    }

    /// <summary>
    /// Проверяет, содержит ли строка хотя бы один из указанных фрагментов.
    /// </summary>
    private static bool ContainsAny(string value, params string[] fragments)
    {
      return fragments.Any(fragment => value.Contains(fragment, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Возвращает часть текста после указанного префикса.
    /// </summary>
    private static string? GetTextAfterPrefix(string? text, string prefix)
    {
      if (string.IsNullOrWhiteSpace(text))
        return null;

      int index = text.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
      return index < 0 ? null : TrimCandidate(text.Substring(index + prefix.Length));
    }

    /// <summary>
    /// Обрезает текст перед указанным маркером.
    /// </summary>
    private static string TrimBefore(string value, string marker)
    {
      int index = value.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
      return index >= 0 ? TrimCandidate(value.Substring(0, index)) : TrimCandidate(value);
    }

    /// <summary>
    /// Удаляет служебные пробелы и знаки пунктуации по краям кандидата.
    /// </summary>
    private static string TrimCandidate(string value)
    {
      return (value ?? string.Empty).Trim().Trim('.', ',', ';', ':', ' ', '"', '\'');
    }

    /// <summary>
    /// Смысловой тип диагностики, используемый для выбора диапазона подсветки.
    /// </summary>
    [Flags]
    private enum IssueKind
    {
      /// <summary>
      /// Тип диагностики не определён.
      /// </summary>
      None = 0,

      /// <summary>
      /// Диагностика относится к сопротивлению.
      /// </summary>
      Resistance = 1,

      /// <summary>
      /// Диагностика относится к напряжению.
      /// </summary>
      Voltage = 2,

      /// <summary>
      /// Диагностика относится к ёмкости.
      /// </summary>
      Capacity = 4,

      /// <summary>
      /// Диагностика относится к силе тока.
      /// </summary>
      Amperage = 8,

      /// <summary>
      /// Диагностика относится ко времени.
      /// </summary>
      Time = 16,

      /// <summary>
      /// Диагностика относится к блоку точек.
      /// </summary>
      Points = 32,

      /// <summary>
      /// Диагностика относится к параметрам команды.
      /// </summary>
      Parameters = 64,

      /// <summary>
      /// Диагностика относится к недопустимой последовательности ",*".
      /// </summary>
      CommaStar = 128,

      /// <summary>
      /// Диагностика относится к отсутствующему обязательному содержимому.
      /// </summary>
      RequiredContent = 256
    }

    /// <summary>
    /// Связывает смысловой тип диагностики с регулярным выражением значения.
    /// </summary>
    private readonly struct ValuePattern
    {
      /// <summary>
      /// Создаёт правило поиска значения.
      /// </summary>
      /// <param name="kind">Смысловой тип диагностики.</param>
      /// <param name="regex">Регулярное выражение для поиска значения.</param>
      public ValuePattern(IssueKind kind, Regex regex)
      {
        Kind = kind;
        Regex = regex;
      }

      /// <summary>
      /// Смысловой тип диагностики.
      /// </summary>
      public IssueKind Kind { get; }

      /// <summary>
      /// Регулярное выражение для поиска значения.
      /// </summary>
      public Regex Regex { get; }
    }
  }
}
