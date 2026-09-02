using Ask.Core.Shared.DTO.Devices.Breakdown;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
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

    Match statusMatch = StatusRegex().Match(response);
    Match measurement = MeasurementRegex().Matches(response).LastOrDefault() ?? Match.Empty;
    if (!statusMatch.Success
      || !Enum.TryParse(statusMatch.Value, ignoreCase: true, out BreakdownMeasurementStatus status)
      || !measurement.Success
      || !double.TryParse(
        measurement.Groups["value"].Value.Replace(',', '.'),
        NumberStyles.Float,
        CultureInfo.InvariantCulture,
        out double value))
      return false;

    result = new BreakdownMeasurementResponse(
      status,
      value,
      measurement.Groups["unit"].Value);
    return true;
  }

  [GeneratedRegex(@"\b(?:PASS|FAIL|TEST)\b", RegexOptions.IgnoreCase)]
  private static partial Regex StatusRegex();

  [GeneratedRegex(@"(?<value>[-+]?(?:\d+(?:[.,]\d*)?|[.,]\d+)(?:[eE][-+]?\d+)?)\s*(?<unit>GOhm|MOhm|kOhm|Ohm|kV|V|mA|uA|µA|A)", RegexOptions.IgnoreCase)]
  private static partial Regex MeasurementRegex();
}
