namespace NewCore.Base.Function.FastMeter
{
  /// <summary>
  /// Интерфейс для измерения ёмкости.
  /// </summary>
  public interface ICapacitanceMeasurement
  {
    /// <summary>
    /// Устанавливает режим измерения ёмкости.
    /// </summary>
    Task SetCapacitanceModeAsync();

    /// <summary>
    /// Выполняет измерение ёмкости.
    /// </summary>
    /// <param name="param">Ожиданемео знчение.</param>
    Task<double> MeasureCapacitanceAsync(double param = 0);
  }
}
