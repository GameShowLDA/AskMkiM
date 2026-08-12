using System.Globalization;
using System.Text.RegularExpressions;

namespace Ask.Device.ResponseProcessor.BreakdownTester.ResponseProcessing.Checkers;

/// <summary>
/// Преобразует числовые ответы пробойной установки.
/// </summary>
internal static partial class NumericResponseChecker
{
  public static bool TryParse(string response, out double value)
  {
    value = default;
    if (string.IsNullOrWhiteSpace(response))
      return false;

    Match match = NumberRegex().Match(response);
    return match.Success
      && double.TryParse(
        match.Groups["value"].Value.Replace(',', '.'),
        NumberStyles.Float,
        CultureInfo.InvariantCulture,
        out value);
  }

  [GeneratedRegex(@"^\s*(?<value>[-+]?(?:\d+(?:[.,]\d*)?|[.,]\d+)(?:[eE][-+]?\d+)?)\s*(?:[a-zA-ZµΩ]+)?\s*$")]
  private static partial Regex NumberRegex();
}
