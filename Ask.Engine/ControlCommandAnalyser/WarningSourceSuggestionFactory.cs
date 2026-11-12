using Ask.Core.Services.Errors.Models;
using Ask.Core.Shared.DTO.Executor;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Model.Ie;
using Ask.Engine.ControlCommandAnalyser.Model.Pr;
using Ask.Engine.ControlCommandAnalyser.Parser.Common.Helpers;
using System.Text.RegularExpressions;

namespace Ask.Engine.ControlCommandAnalyser;

/// <summary>
/// Формирует безопасные исправления исходного текста для предупреждений с однозначным результатом.
/// </summary>
internal static class WarningSourceSuggestionFactory
{
  private static readonly Regex VoltageRegex = new(
    @"(?<!\d[.,])\d+(?:[.,]\d+)?\s*(?:В|кВ|КВ|мВ|МВ)\b",
    RegexOptions.Compiled | RegexOptions.IgnoreCase);

  private static readonly Regex AmperageRegex = new(
    @"\d+(?:[.,]\d+)?\s*(?:А|кА|КА|мА|МА)\b",
    RegexOptions.Compiled | RegexOptions.IgnoreCase);

  private static readonly Regex CapacityRegex = new(
    @"(?:(?<low>\d+(?:[.,]\d+)?)\s*<\s*)?(?<unit>нф|мкф|пф)(?:\s*<\s*(?<high>\d+(?:[.,]\d+)?))?",
    RegexOptions.Compiled | RegexOptions.IgnoreCase);

  public static SourceTextSuggestion? Create(
    IReadOnlyList<(int Offset, string Text)> lines,
    BaseCommandModel model,
    WarningItem warning,
    int? endLineNumber = null)
  {
    if (warning.Code == WarningCode.Unknown)
    {
      return CreateCompletedRangeSuggestion(
        lines,
        model.StartLineNumber,
        endLineNumber ?? lines.Count,
        warning);
    }

    if (!TryGetLine(lines, warning.SourceLineNumber, out var line))
    {
      return null;
    }

    return warning.Code switch
    {
      WarningCode.Gen_KeyZR when model is PrCommandModel pr =>
        CommandTranslationManager.CreatePrAlgorithmKeySuggestion(lines, pr, "ЗР"),
      WarningCode.Gen_DuplicateKey => CreateDuplicateKeySuggestion(line, warning),
      WarningCode.Gen_IgnoreVoltage => CreateRemovedParameterSuggestion(line, VoltageRegex),
      WarningCode.Gen_IgnoreAmperage => CreateRemovedParameterSuggestion(line, AmperageRegex),
      WarningCode.Gen_DefaultResistainceLowLimit when model is SiCommandModel si =>
        CreateInsertedParameterSuggestion(line, si.ResistanceSource),
      WarningCode.Gen_DefaultResistainceHighLimit when model is IeCommandModel ie =>
        CreateCapacityRangeSuggestion(line, ie),
      WarningCode.Gen_DefaultTime => CreateDefaultTimeSuggestion(line),
      WarningCode.Gen_PeToPrCommandAddedAutomatically when model is PrCommandModel pr =>
        CreatePeToPrSuggestion(line, pr),
      _ => null,
    };
  }

  private static SourceTextSuggestion? CreateDuplicateKeySuggestion(
    (int Offset, string Text) line,
    WarningItem warning)
  {
    int separator = warning.Description.LastIndexOf(':');
    string key = separator >= 0 ? warning.Description[(separator + 1)..].Trim().TrimEnd('.') : string.Empty;
    if (string.IsNullOrWhiteSpace(key))
    {
      return null;
    }

    int headerEnd = FindHeaderEnd(line.Text);
    string header = line.Text[..headerEnd];
    var matches = Regex.Matches(
      header,
      $@"(?<![\p{{L}}\p{{N}}/]){Regex.Escape(key)}(?![\p{{L}}\p{{N}}/])",
      RegexOptions.IgnoreCase);
    if (matches.Count < 2)
    {
      return null;
    }

    string corrected = line.Text;
    for (int index = matches.Count - 1; index >= 1; index--)
    {
      var span = ExpandDelimitedSpan(corrected, matches[index].Index, matches[index].Length);
      corrected = corrected.Remove(span.Start, span.Length);
    }

    return CreateLineReplacement(line, corrected);
  }

  private static SourceTextSuggestion? CreateRemovedParameterSuggestion(
    (int Offset, string Text) line,
    Regex parameterRegex)
  {
    int headerEnd = FindHeaderEnd(line.Text);
    var match = parameterRegex.Match(line.Text[..headerEnd]);
    if (!match.Success)
    {
      return null;
    }

    var span = ExpandDelimitedSpan(line.Text, match.Index, match.Length);
    string corrected = line.Text.Remove(span.Start, span.Length);
    return CreateLineReplacement(line, corrected);
  }

  private static SourceTextSuggestion? CreateInsertedParameterSuggestion(
    (int Offset, string Text) line,
    string? parameter)
  {
    if (string.IsNullOrWhiteSpace(parameter)
      || !TryFindParameterInsertion(line.Text, out int localOffset, out string prefix))
    {
      return null;
    }

    string insertion = prefix + parameter;
    return new SourceTextSuggestion(
      line.Offset + localOffset,
      insertion,
      insertion);
  }

  private static SourceTextSuggestion? CreateCapacityRangeSuggestion(
    (int Offset, string Text) line,
    IeCommandModel model)
  {
    if (!model.LowerLimitCapacity.HasValue
      || !model.HigherLimitCapacity.HasValue
      || string.IsNullOrWhiteSpace(model.CapacityUnit))
    {
      return null;
    }

    var match = CapacityRegex.Match(line.Text);
    if (!match.Success || !match.Groups["low"].Success || match.Groups["high"].Success)
    {
      return null;
    }

    string replacement =
      $"{MeasurementSourceValueFormatter.FormatValue(model.LowerLimitCapacity.Value)}" +
      $"<{model.CapacityUnit}<" +
      MeasurementSourceValueFormatter.FormatValue(model.HigherLimitCapacity.Value);
    string corrected = line.Text.Remove(match.Index, match.Length).Insert(match.Index, replacement);
    return CreateLineReplacement(line, corrected);
  }

  private static SourceTextSuggestion? CreateDefaultTimeSuggestion((int Offset, string Text) line)
  {
    var match = Regex.Match(
      line.Text,
      @"(?<=^|\s)(?<unit>м[сc]|ms|с|c)\b",
      RegexOptions.IgnoreCase);
    if (!match.Success)
    {
      return null;
    }

    string corrected = line.Text.Insert(match.Index, "1");
    return CreateLineReplacement(line, corrected);
  }

  private static SourceTextSuggestion? CreatePeToPrSuggestion(
    (int Offset, string Text) line,
    PrCommandModel model)
  {
    var header = Regex.Match(
      line.Text,
      @"^\s*\d+\s+(?<mnemonic>ПЭ)(?=\s|$)",
      RegexOptions.IgnoreCase);
    string? resistance = model.SourceLines.Skip(1).FirstOrDefault()?.Trim();
    if (!header.Success || string.IsNullOrWhiteSpace(resistance))
    {
      return null;
    }

    string corrected = line.Text
      .Remove(header.Groups["mnemonic"].Index, header.Groups["mnemonic"].Length)
      .Insert(header.Groups["mnemonic"].Index, "ПР");
    if (!TryFindParameterInsertion(corrected, out int insertionOffset, out string prefix))
    {
      return null;
    }

    corrected = corrected.Insert(insertionOffset, prefix + resistance);
    return CreateLineReplacement(line, corrected);
  }

  private static SourceTextSuggestion? CreateCompletedRangeSuggestion(
    IReadOnlyList<(int Offset, string Text)> lines,
    int startLineNumber,
    int endLineNumber,
    WarningItem warning)
  {
    var description = Regex.Match(
      warning.Description,
      @"^Запись диапазона \((?<token>.+)\) была дополнена\.",
      RegexOptions.IgnoreCase);
    if (!description.Success)
    {
      return null;
    }

    string token = description.Groups["token"].Value;
    var range = Regex.Match(token, @"^(?<left>.+?)(?<leftNumber>\d+)-(?<rightNumber>\d+)$");
    if (!range.Success)
    {
      return null;
    }

    string left = range.Groups["left"].Value + range.Groups["leftNumber"].Value;
    string prefix = range.Groups["left"].Value;
    string completed = $"{left}-{prefix}{range.Groups["rightNumber"].Value}";
    int firstIndex = Math.Max(0, startLineNumber - 1);
    int lastIndex = Math.Min(lines.Count, endLineNumber);
    for (int lineIndex = firstIndex; lineIndex < lastIndex; lineIndex++)
    {
      var line = lines[lineIndex];
      int tokenIndex = line.Text.IndexOf(token, StringComparison.OrdinalIgnoreCase);
      if (tokenIndex < 0)
      {
        continue;
      }

      string corrected = line.Text.Remove(tokenIndex, token.Length).Insert(tokenIndex, completed);
      return CreateLineReplacement(line, corrected);
    }

    return null;
  }

  private static bool TryGetLine(
    IReadOnlyList<(int Offset, string Text)> lines,
    int lineNumber,
    out (int Offset, string Text) line)
  {
    int index = lineNumber - 1;
    if (index >= 0 && index < lines.Count)
    {
      line = lines[index];
      return true;
    }

    line = default;
    return false;
  }

  private static bool TryFindParameterInsertion(string line, out int offset, out string prefix)
  {
    var header = Regex.Match(line, @"^\s*\d+\s+[\p{L}_]+(?=\s|$)");
    if (!header.Success)
    {
      offset = 0;
      prefix = string.Empty;
      return false;
    }

    int boundary = FindHeaderEnd(line);
    while (boundary > header.Length && char.IsWhiteSpace(line[boundary - 1]))
    {
      boundary--;
    }

    string body = line[header.Length..boundary].Trim();
    offset = boundary;
    prefix = body.Length == 0 ? " " : body.EndsWith(',') ? " " : ", ";
    return true;
  }

  private static int FindHeaderEnd(string line)
  {
    int result = line.Length;
    foreach (string marker in new[] { "/*", "//", "{", "*" })
    {
      int index = line.IndexOf(marker, StringComparison.Ordinal);
      if (index >= 0 && index < result)
      {
        result = index;
      }
    }
    return result;
  }

  private static (int Start, int Length) ExpandDelimitedSpan(string text, int start, int length)
  {
    int end = start + length;
    while (end < text.Length && char.IsWhiteSpace(text[end])) end++;
    if (end < text.Length && text[end] == ',')
    {
      end++;
      while (end < text.Length && char.IsWhiteSpace(text[end])) end++;
      return (start, end - start);
    }

    int left = start;
    while (left > 0 && char.IsWhiteSpace(text[left - 1])) left--;
    if (left > 0 && text[left - 1] == ',')
    {
      left--;
      while (left > 0 && char.IsWhiteSpace(text[left - 1])) left--;
      return (left, start + length - left);
    }

    return (start, end - start);
  }

  private static SourceTextSuggestion? CreateLineReplacement(
    (int Offset, string Text) line,
    string corrected)
  {
    if (string.Equals(line.Text, corrected, StringComparison.Ordinal))
    {
      return null;
    }

    return new SourceTextSuggestion(
      line.Offset,
      corrected,
      $"  → {corrected.TrimStart()}",
      ReplacementLength: line.Text.Length,
      PreviewOffset: line.Offset + line.Text.Length);
  }
}
