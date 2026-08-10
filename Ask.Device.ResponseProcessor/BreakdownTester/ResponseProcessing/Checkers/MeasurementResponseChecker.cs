using Ask.Device.ResponseProcessor.BreakdownTester.ResponseModels;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Ask.Device.ResponseProcessor.BreakdownTester.ResponseProcessing.Checkers;

/// <summary>
/// Преобразует ответы измерения пробойной установки.
/// </summary>
internal static partial class MeasurementResponseChecker
{
  public static bool TryParse(string response, out BreakdownMeasurementResponse? result)
  {
    result = null;
    if (string.IsNullOrWhiteSpace(response))
      return false;

    string status = StatusRegex().Match(response).Value.ToUpperInvariant();
    Match measurement = MeasurementRegex().Matches(response).LastOrDefault() ?? Match.Empty;
    if (!measurement.Success
      || !double.TryParse(
        measurement.Groups["value"].Value.Replace(',', '.'),
        NumberStyles.Float,
        CultureInfo.InvariantCulture,
        out double value))
      return false;

    result = new BreakdownMeasurementResponse(
      string.IsNullOrEmpty(status) ? "UNKNOWN" : status,
      value,
      measurement.Groups["unit"].Value);
    return true;
  }

  [GeneratedRegex(@"\b(?:PASS|FAIL|TEST|READY|STOP)\b", RegexOptions.IgnoreCase)]
  private static partial Regex StatusRegex();

  [GeneratedRegex(@"(?<value>[-+]?(?:\d+(?:[.,]\d*)?|[.,]\d+)(?:[eE][-+]?\d+)?)\s*(?<unit>GOhm|MOhm|kOhm|Ohm|kV|V|mA|uA|µA|A)", RegexOptions.IgnoreCase)]
  private static partial Regex MeasurementRegex();
}
