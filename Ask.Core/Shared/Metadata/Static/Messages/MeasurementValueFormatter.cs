using System.Globalization;

namespace Ask.Core.Shared.Metadata.Static.Messages
{
  /// <summary>
  /// Выполняет единое округление и строковое форматирование результатов измерений.
  /// </summary>
  public static class MeasurementValueFormatter
  {
    private const int DisplayPrecision = 3;
    private const string DisplayFormat = "0.000";

    /// <summary>
    /// Округляет измеренное значение для отображения с точностью до трёх знаков после запятой.
    /// </summary>
    public static double Round(double value)
    {
      if (double.IsNaN(value) || double.IsInfinity(value) || value >= 9.9E+37)
      {
        return value;
      }

      return Math.Round(value, DisplayPrecision, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Формирует строку измеренного значения с округлением до трёх знаков после запятой.
    /// </summary>
    public static string Format(double value)
    {
      return Round(value).ToString(DisplayFormat, CultureInfo.CurrentCulture);
    }

    /// <summary>
    /// Формирует строку измеренного значения с единицей измерения.
    /// </summary>
    public static string FormatWithUnit(double value, string unit)
    {
      return $"{Format(value)} {unit}";
    }

    /// <summary>
    /// Определяет, соответствует ли значение признаку перегрузки прибора.
    /// </summary>
    public static bool IsOverloadValue(double value, double threshold = 9.9E+37)
    {
      return value >= threshold;
    }
  }
}
