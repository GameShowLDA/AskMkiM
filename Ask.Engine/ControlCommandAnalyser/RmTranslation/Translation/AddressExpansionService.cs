using Ask.Engine.ControlCommandAnalyser.RmTranslation.Diagnostics;
using Ask.Engine.ControlCommandAnalyser.RmTranslation.Models;
using System.Globalization;

namespace Ask.Engine.ControlCommandAnalyser.RmTranslation.Translation;

public sealed class AddressExpansionService
{
  public Result<ObjectAddressRange> ExpandObjectExpression(AddressExpressionSyntax expression)
  {
    var diagnostics = new List<RmDiagnostic>();
    var addresses = new List<ObjectAddress>();

    foreach (var item in SplitTopLevel(expression.Text, ','))
    {
      var expanded = ExpandSingleObjectExpression(item, expression.Span, diagnostics);
      addresses.AddRange(expanded.Select(value => new ObjectAddress(value)));
    }

    if (addresses.Count == 0 && diagnostics.Count == 0)
    {
      diagnostics.Add(RmDiagnostic.Error(
        RmDiagnosticCode.InvalidObjectAddress,
        $"Некорректный формат адреса объекта контроля: {expression.Text}.",
        expression.Span));
    }

    return new Result<ObjectAddressRange>(
      new ObjectAddressRange(expression.Text, addresses),
      diagnostics);
  }

  public Result<IReadOnlyList<MachineAddress>> ExpandMachineExpression(AddressExpressionSyntax expression)
  {
    var diagnostics = new List<RmDiagnostic>();

    if (!TryParseMachineAddressRange(expression.Text, expression.Span, diagnostics, out var range))
      return new Result<IReadOnlyList<MachineAddress>>(Array.Empty<MachineAddress>(), diagnostics);

    return new Result<IReadOnlyList<MachineAddress>>(range.Expand(), diagnostics);
  }

  private static IReadOnlyList<string> ExpandSingleObjectExpression(
    string expression,
    TextSpan span,
    List<RmDiagnostic> diagnostics)
  {
    expression = expression.Trim();
    if (expression.Length == 0)
      return Array.Empty<string>();

    var separator = FindTopLevelRangeSeparator(expression);
    if (separator < 0)
      return new[] { expression };

    var left = expression[..separator];
    var right = expression[(separator + 1)..];
    var rangeTail = ExtractRangeTail(right);
    right = rangeTail.Core;

    if (TryExpandDecimalRange(left, right, rangeTail, out var decimalValues))
      return decimalValues;

    if (TryExpandIntegerSuffixRange(left, right, rangeTail, out var integerValues))
      return integerValues;

    diagnostics.Add(RmDiagnostic.Error(
      RmDiagnosticCode.InvalidRange,
      $"Некорректный диапазон адресов объекта контроля: {expression}.",
      span));
    return Array.Empty<string>();
  }

  private static bool TryExpandDecimalRange(
    string left,
    string right,
    RangeTail tail,
    out IReadOnlyList<string> values)
  {
    values = Array.Empty<string>();
    if (!TrySplitTrailingNumber(left, allowDecimal: true, out var leftPrefix, out var leftNumberText)
      || !decimal.TryParse(leftNumberText, NumberStyles.Number, CultureInfo.InvariantCulture, out var start))
    {
      return false;
    }

    if (!TrySplitTrailingNumber(right, allowDecimal: true, out var rightPrefix, out var rightNumberText)
      || !decimal.TryParse(rightNumberText, NumberStyles.Number, CultureInfo.InvariantCulture, out var end))
    {
      return false;
    }

    if (rightPrefix.Length > 0
      && !string.Equals(leftPrefix, rightPrefix, StringComparison.OrdinalIgnoreCase)
      && !leftPrefix.EndsWith(rightPrefix, StringComparison.OrdinalIgnoreCase))
    {
      return false;
    }

    if (!leftNumberText.Contains('.') && !rightNumberText.Contains('.'))
      return false;

    var step = tail.StepText is null
      ? (end >= start ? 1m : -1m)
      : decimal.Parse(tail.StepText, NumberStyles.Number, CultureInfo.InvariantCulture);

    if (step == 0 || Math.Sign(end - start) != 0 && Math.Sign(end - start) != Math.Sign(step))
      return false;

    var precision = new[] { leftNumberText, rightNumberText, tail.StepText ?? "1" }
      .Select(GetPrecision)
      .Max();
    var format = precision == 0 ? "0" : "0." + new string('0', precision);

    var result = new List<string>();
    for (var current = start; step > 0 ? current <= end : current >= end; current += step)
      result.Add($"{leftPrefix}{current.ToString(format, CultureInfo.InvariantCulture)}{tail.Suffix}");

    values = result;
    return true;
  }

  private static bool TryExpandIntegerSuffixRange(
    string left,
    string right,
    RangeTail tail,
    out IReadOnlyList<string> values)
  {
    values = Array.Empty<string>();
    if (!TrySplitTrailingNumber(left, allowDecimal: false, out var leftPrefix, out var leftNumberText)
      || !int.TryParse(leftNumberText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var start))
    {
      return false;
    }

    string rightNumberText;
    var rightPrefix = string.Empty;
    if (TrySplitTrailingNumber(right, allowDecimal: false, out var parsedRightPrefix, out var parsedRightNumberText))
    {
      rightPrefix = parsedRightPrefix;
      rightNumberText = parsedRightNumberText;
    }
    else
    {
      rightNumberText = right;
    }

    if (!int.TryParse(rightNumberText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var end))
      return false;

    if (rightPrefix.Length > 0
      && !string.Equals(leftPrefix, rightPrefix, StringComparison.OrdinalIgnoreCase)
      && !leftPrefix.EndsWith(rightPrefix, StringComparison.OrdinalIgnoreCase))
    {
      return false;
    }

    var rawStep = tail.StepText is null
      ? 1
      : int.Parse(tail.StepText, NumberStyles.Integer, CultureInfo.InvariantCulture);
    if (rawStep == 0)
      return false;

    var direction = end >= start ? 1 : -1;
    var step = Math.Abs(rawStep) * direction;
    var result = new List<string>();
    for (var current = start; direction > 0 ? current <= end : current >= end; current += step)
      result.Add($"{leftPrefix}{current}{tail.Suffix}");

    values = result;
    return true;
  }

  private static bool TryParseMachineAddressRange(
    string expression,
    TextSpan span,
    List<RmDiagnostic> diagnostics,
    out MachineAddressRange range)
  {
    range = new MachineAddressRange(default, default);
    var separator = FindTopLevelRangeSeparator(expression);
    if (separator < 0)
    {
      if (!TryParseMachineAddress(expression, out var single))
      {
        diagnostics.Add(RmDiagnostic.Error(
          RmDiagnosticCode.InvalidMachineAddress,
          $"Некорректный формат машинного адреса: {expression}.",
          span));
        return false;
      }

      range = new MachineAddressRange(single, single);
      return true;
    }

    var leftText = expression[..separator];
    var rightTail = ExtractRangeTail(expression[(separator + 1)..]);
    if (!TryParseMachineAddress(leftText, out var start))
    {
      diagnostics.Add(RmDiagnostic.Error(
        RmDiagnosticCode.InvalidMachineAddress,
        $"Некорректный формат машинного адреса: {leftText}.",
        span));
      return false;
    }

    MachineAddress end;
    if (rightTail.Core.Contains('.'))
    {
      if (!TryParseMachineAddress(rightTail.Core, out end))
      {
        diagnostics.Add(RmDiagnostic.Error(
          RmDiagnosticCode.InvalidMachineAddress,
          $"Некорректный формат машинного адреса: {rightTail.Core}.",
          span));
        return false;
      }
    }
    else if (int.TryParse(rightTail.Core, NumberStyles.Integer, CultureInfo.InvariantCulture, out var endPoint))
    {
      end = start with { Point = endPoint };
    }
    else
    {
      diagnostics.Add(RmDiagnostic.Error(
        RmDiagnosticCode.InvalidMachineAddress,
        $"Некорректный формат машинного адреса: {expression}.",
        span));
      return false;
    }

    if (start.Rack != end.Rack || start.Block != end.Block)
    {
      diagnostics.Add(RmDiagnostic.Error(
        RmDiagnosticCode.InvalidRange,
        "Диапазон машинных адресов не может пересекать стойки или блоки.",
        span));
      return false;
    }

    var rawStep = rightTail.StepText is null
      ? 1
      : int.Parse(rightTail.StepText, NumberStyles.Integer, CultureInfo.InvariantCulture);
    if (rawStep == 0)
    {
      diagnostics.Add(RmDiagnostic.Error(
        RmDiagnosticCode.InvalidRange,
        "Шаг диапазона не может быть равен нулю.",
        span));
      return false;
    }

    var direction = end.Point >= start.Point ? 1 : -1;
    range = new MachineAddressRange(start, end, Math.Abs(rawStep) * direction);
    return true;
  }

  private static bool TryParseMachineAddress(string text, out MachineAddress address)
  {
    address = default;
    var parts = text.Split('.');
    if (parts.Length != 3)
      return false;

    if (!int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var rack)
      || !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var block)
      || !int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var point)
      || rack <= 0
      || block <= 0
      || point <= 0)
    {
      return false;
    }

    address = new MachineAddress(rack, block, point);
    return true;
  }

  private static int FindTopLevelRangeSeparator(string text)
  {
    var depth = 0;
    for (var i = 0; i < text.Length; i++)
    {
      var current = text[i];
      if (current == '(')
        depth++;
      else if (current == ')')
        depth = Math.Max(0, depth - 1);
      else if (current == '-' && depth == 0 && i > 0 && i + 1 < text.Length)
        return i;
    }

    return -1;
  }

  private static RangeTail ExtractRangeTail(string text)
  {
    var open = text.IndexOf('(');
    if (open < 0)
      return new RangeTail(text, null, string.Empty);

    var close = text.IndexOf(')', open + 1);
    if (close < 0)
      return new RangeTail(text, null, string.Empty);

    return new RangeTail(
      text[..open],
      text[(open + 1)..close],
      text[(close + 1)..]);
  }

  private static bool TrySplitTrailingNumber(string text, bool allowDecimal, out string prefix, out string numberText)
  {
    prefix = string.Empty;
    numberText = string.Empty;
    if (text.Length == 0)
      return false;

    var end = text.Length - 1;
    if (!char.IsDigit(text[end]))
      return false;

    var start = end;
    var dotSeen = false;
    while (start >= 0)
    {
      var current = text[start];
      if (char.IsDigit(current))
      {
        start--;
        continue;
      }

      if (allowDecimal && current == '.' && !dotSeen)
      {
        dotSeen = true;
        start--;
        continue;
      }

      break;
    }

    prefix = text[..(start + 1)];
    numberText = text[(start + 1)..];
    return numberText.Length > 0;
  }

  private static IReadOnlyList<string> SplitTopLevel(string text, char separator)
  {
    var result = new List<string>();
    var depth = 0;
    var start = 0;

    for (var i = 0; i < text.Length; i++)
    {
      var current = text[i];
      if (current == '(')
        depth++;
      else if (current == ')')
        depth = Math.Max(0, depth - 1);
      else if (current == separator && depth == 0)
      {
        var item = text[start..i].Trim();
        if (item.Length > 0)
          result.Add(item);
        start = i + 1;
      }
    }

    var last = text[start..].Trim();
    if (last.Length > 0)
      result.Add(last);

    return result;
  }

  private static int GetPrecision(string value)
  {
    var dot = value.IndexOf('.');
    return dot < 0 ? 0 : value.Length - dot - 1;
  }

  private sealed record RangeTail(string Core, string? StepText, string Suffix);
}
