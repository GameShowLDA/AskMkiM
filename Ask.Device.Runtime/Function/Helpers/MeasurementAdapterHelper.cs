namespace Ask.Device.Runtime.Function.Helpers
{
  /// <summary>
  /// Выполняет нормализацию измеренных значений на уровне драйвера/адаптера.
  /// </summary>
  internal static class MeasurementAdapterHelper
  {
    private const int DisplayPrecision = 3;

    /// <summary>
    /// Округляет измеренное значение до трёх знаков после запятой.
    /// Специальные значения перегрузки и нечисловые значения не изменяются.
    /// </summary>
    public static double Round(double value)
    {
      if (double.IsNaN(value) || double.IsInfinity(value) || value >= 9.9E+37)
      {
        return value;
      }

      return Math.Round(value, DisplayPrecision, MidpointRounding.AwayFromZero);
    }
  }
}
