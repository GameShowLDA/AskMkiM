using System.Globalization;

namespace Ask.Engine.ControlCommandAnalyser.Parser.Common.Helpers
{
  internal static class MeasurementSourceValueFormatter
  {
    private static readonly CultureInfo DisplayCulture = CultureInfo.GetCultureInfo("ru-RU");

    public static string FormatValue(double value)
    {
      return value.ToString("G", DisplayCulture);
    }

    public static string FormatWithSpace(double value, string? unit)
    {
      return string.IsNullOrEmpty(unit)
        ? FormatValue(value)
        : $"{FormatValue(value)} {unit}";
    }

    public static string FormatCompact(double value, string? unit)
    {
      return $"{FormatValue(value)}{unit}";
    }
  }
}
