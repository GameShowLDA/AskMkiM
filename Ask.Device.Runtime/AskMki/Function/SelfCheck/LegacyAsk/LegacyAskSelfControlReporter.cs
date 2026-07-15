using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Ask.Engine.Tests.SelfControl;

/// <summary>
/// Пишет ход самоконтроля старой АСК в стандартный протокол АСКМ.
/// </summary>
public sealed class LegacyAskSelfControlReporter
{
  private static readonly Regex ExpectedValueRegex = new(
    @"д\.быть\s*=\s*(?<expected>[+-]?\d+(?:[.,]\d+)?)\s*(?<unit>мВ|В|мА|А|Ом|кОм|МОм|ГОм)\s*(?:\+-\s*(?<tolerance>[+-]?\d+(?:[.,]\d+)?)\s*(?<toleranceUnit>мВ|В|мА|А|Ом|кОм|МОм|ГОм))?",
    RegexOptions.IgnoreCase | RegexOptions.Compiled);

  private static readonly Regex ExpectedLimitRegex = new(
    @"д\.быть\s*(?<operator>>=|<=|>|<)\s*(?<expected>[+-]?\d+(?:[.,]\d+)?)\s*(?<unit>мВ|В|мА|А|Ом|кОм|МОм|ГОм)",
    RegexOptions.IgnoreCase | RegexOptions.Compiled);

  private static readonly Regex MeasuredValueRegex = new(
    @"(?<name>Uизм|Rизм|Iизм|Uацп|Uв7)\s*(?<operator>>=|<=|>|<|=)?\s*(?<value>[+-]?\d+(?:[.,]\d+)?)(?<unit>мВ|В|мА|А|Ом|кОм|МОм|ГОм)?",
    RegexOptions.IgnoreCase | RegexOptions.Compiled);

  private static readonly Regex RangeRegex = new(
    @"Диап(?:U)?\s*=\s*(?<range>[+-]?\d+(?:[.,]\d+)?)\s*(?<unit>мВ|В|Ом|кОм|МОм|ГОм)",
    RegexOptions.IgnoreCase | RegexOptions.Compiled);

  private readonly IUserInteractionService _messageService;

  public LegacyAskSelfControlReporter(IUserInteractionService messageService)
  {
    _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
  }

  public bool HasFailedMeasurements { get; private set; }

  public Task BeginTestAsync(string testName)
  {
    HasFailedMeasurements = false;
    return WriteAsync(testName, null, ShowMessageModel.MessageType.Command, indentLevel: 0, isCommandHeader: true);
  }

  public Task EndTestAsync(string testName)
  {
    return WriteAsync("Тест завершен", testName, ShowMessageModel.MessageType.Info, indentLevel: 0);
  }

  public Task CompleteCommandAsync(bool hasErrors)
  {
    return _messageService.CompleteCommandAsync(hasErrors);
  }

  public Task BeginSubTestAsync(string testTitle, int number, string testName)
  {
    return WriteAsync($"{number}. {testName}", testTitle, ShowMessageModel.MessageType.CommandBlock, indentLevel: 1);
  }

  public Task EndSubTestAsync(string testTitle, int number, string testName)
  {
    return Task.CompletedTask;
  }

  public Task TestStepAsync(string message)
  {
    bool? passed = EvaluateMeasurementLine(message);
    return passed.HasValue
      ? TestStepAsync(message, passed.Value)
      : WriteAsync(null, message, ShowMessageModel.MessageType.Info, indentLevel: 2);
  }

  public Task TestStepAsync(string message, bool passed)
  {
    if (!passed)
    {
      HasFailedMeasurements = true;
    }

    string checkedMessage = HasStatusMarker(message)
      ? message
      : message + (passed ? " [НОРМА]" : " [БРАК]");

    return WriteAsync(null, checkedMessage, passed ? ShowMessageModel.MessageType.Success : ShowMessageModel.MessageType.Error, indentLevel: 2);
  }

  public Task DocumentAsync(string message)
  {
    return WriteAsync(message, null, ShowMessageModel.MessageType.Info, indentLevel: 1);
  }

  public Task SuccessAsync(string message)
  {
    return WriteAsync("Результат", message, ShowMessageModel.MessageType.Success, indentLevel: 1);
  }

  public Task ErrorAsync(string message)
  {
    return WriteAsync("Ошибка", message, ShowMessageModel.MessageType.Error, indentLevel: 1);
  }

  public async Task WriteSummaryAsync(
    string title,
    bool isIdleMode,
    DateTime startedAt,
    TimeSpan elapsed,
    bool hasErrors)
  {
    string mode = isIdleMode ? "Холостой режим" : "Боевой режим";
    string started = startedAt.ToString("dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture);
    string duration = elapsed.ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture);
    string result = hasErrors ? "БРАК [БРАК]" : "НОРМА [НОРМА]";

    await WriteAsync("Итог самоконтроля", title, hasErrors ? ShowMessageModel.MessageType.Error : ShowMessageModel.MessageType.Success, indentLevel: 0);
    await WriteAsync("Режим", mode, ShowMessageModel.MessageType.Info, indentLevel: 1);
    await WriteAsync("Начало выполнения", started, ShowMessageModel.MessageType.Info, indentLevel: 1);
    await WriteAsync("Время выполнения", duration, ShowMessageModel.MessageType.Info, indentLevel: 1);
    await WriteAsync("Результат", result, hasErrors ? ShowMessageModel.MessageType.Error : ShowMessageModel.MessageType.Success, indentLevel: 1);
  }

  private static bool? EvaluateMeasurementLine(string message)
  {
    if (message.Contains("Д.быть перегр.", StringComparison.OrdinalIgnoreCase))
    {
      return EvaluateOverloadLine(message);
    }

    Match expectedMatch = ExpectedValueRegex.Match(message);
    if (!expectedMatch.Success)
    {
      expectedMatch = ExpectedLimitRegex.Match(message);
    }

    if (!expectedMatch.Success)
    {
      return null;
    }

    var measuredMatches = MeasuredValueRegex.Matches(message);
    if (measuredMatches.Count == 0)
    {
      return null;
    }

    double expected = ToBaseUnit(expectedMatch.Groups["expected"].Value, expectedMatch.Groups["unit"].Value);
    string expectedOperator = expectedMatch.Groups["operator"].Success ? expectedMatch.Groups["operator"].Value : "=";
    double tolerance = expectedMatch.Groups["tolerance"].Success
      ? ToBaseUnit(expectedMatch.Groups["tolerance"].Value, expectedMatch.Groups["toleranceUnit"].Value)
      : 0.0;

    foreach (Match measuredMatch in measuredMatches)
    {
      string unit = measuredMatch.Groups["unit"].Success && !string.IsNullOrWhiteSpace(measuredMatch.Groups["unit"].Value)
        ? measuredMatch.Groups["unit"].Value
        : expectedMatch.Groups["unit"].Value;
      double measured = ToBaseUnit(measuredMatch.Groups["value"].Value, unit);
      string measuredOperator = measuredMatch.Groups["operator"].Success ? measuredMatch.Groups["operator"].Value : "=";

      if (!IsMeasurementPassed(expected, tolerance, expectedOperator, measured, measuredOperator))
      {
        return false;
      }
    }

    return true;
  }

  private static bool? EvaluateOverloadLine(string message)
  {
    Match measuredMatch = MeasuredValueRegex.Match(message);
    if (!measuredMatch.Success || !measuredMatch.Groups["operator"].Success)
    {
      return null;
    }

    Match rangeMatch = RangeRegex.Match(message);
    if (!rangeMatch.Success)
    {
      return measuredMatch.Groups["operator"].Value.Contains('>');
    }

    string unit = measuredMatch.Groups["unit"].Success && !string.IsNullOrWhiteSpace(measuredMatch.Groups["unit"].Value)
      ? measuredMatch.Groups["unit"].Value
      : rangeMatch.Groups["unit"].Value;
    double measured = ToBaseUnit(measuredMatch.Groups["value"].Value, unit);
    double range = ToBaseUnit(rangeMatch.Groups["range"].Value, rangeMatch.Groups["unit"].Value);
    return Compare(measured, measuredMatch.Groups["operator"].Value, range);
  }

  private static bool IsMeasurementPassed(double expected, double tolerance, string expectedOperator, double measured, string measuredOperator)
  {
    if (measuredOperator is ">" or ">=" or "<" or "<=")
    {
      return Compare(measured, measuredOperator, expected);
    }

    return expectedOperator switch
    {
      ">" => measured > expected,
      ">=" => measured >= expected,
      "<" => measured < expected,
      "<=" => measured <= expected,
      _ => Math.Abs(measured - expected) <= tolerance
    };
  }

  private static bool Compare(double left, string operation, double right)
  {
    return operation switch
    {
      ">" => left > right,
      ">=" => left >= right,
      "<" => left < right,
      "<=" => left <= right,
      _ => Math.Abs(left - right) < 0.0000001
    };
  }

  private static double ToBaseUnit(string value, string unit)
  {
    double number = double.Parse(value.Replace(',', '.'), CultureInfo.InvariantCulture);
    return unit.ToLowerInvariant() switch
    {
      "мв" => number / 1000.0,
      "в" => number,
      "ма" => number / 1000.0,
      "а" => number,
      "ом" => number,
      "ком" => number * 1000.0,
      "мом" => number * 1000000.0,
      "гом" => number * 1000000000.0,
      _ => number
    };
  }

  private static bool HasStatusMarker(string message)
  {
    return message.Contains("[НОРМА]", StringComparison.OrdinalIgnoreCase)
      || message.Contains("[БРАК]", StringComparison.OrdinalIgnoreCase);
  }

  private Task WriteAsync(
    string? header,
    string? message,
    ShowMessageModel.MessageType type,
    int indentLevel,
    bool isCommandHeader = false)
  {
    var model = new ShowMessageModel(header, message: message, type: type)
    {
      IndentLevel = indentLevel,
      IsControlProgramCommandHeader = isCommandHeader
    };

    return _messageService.ShowMessageAsync(model, skipPause: true);
  }
}
