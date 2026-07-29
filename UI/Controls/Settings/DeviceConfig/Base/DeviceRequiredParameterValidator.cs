using System.Globalization;

namespace UI.Controls.Settings.DeviceConfig.Base;

/// <summary>
/// Shared required-parameter rules for device configuration forms.
/// </summary>
public static class DeviceRequiredParameterValidator
{
  public static string NormalizeConnectionType(string? content, bool isEnabled)
  {
    if (!isEnabled)
    {
      return string.Empty;
    }

    string text = content?.Trim() ?? string.Empty;
    return text is "IP" or "COM" or "USB" ? text : string.Empty;
  }

  public static bool IsValidIpPart(int value) => value is >= 0 and <= 255;

  public static bool IsValidIpAddress(int part1, int part2, int part3, int part4)
  {
    return IsValidIpPart(part1) &&
      IsValidIpPart(part2) &&
      IsValidIpPart(part3) &&
      IsValidIpPart(part4);
  }

  public static bool IsNonNegativeNumber(string? text)
  {
    return TryGetNumber(text, out double value) && value >= 0;
  }

  public static bool IsPositiveNumber(string? text)
  {
    return TryGetNumber(text, out double value) && value > 0;
  }

  private static bool TryGetNumber(string? text, out double value)
  {
    return double.TryParse(
      text?.Trim().Replace(',', '.'),
      NumberStyles.AllowDecimalPoint,
      CultureInfo.InvariantCulture,
      out value);
  }
}
