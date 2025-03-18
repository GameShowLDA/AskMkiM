namespace NewCore.Base.Function.FastMeter
{
  /// <summary>
  /// Интерфейс для измерения переменного напряжения.
  /// </summary>
  public interface IAcVoltageMeasurement
  {
    /// <summary>
    /// Устанавливает режим измерения переменного напряжения.
    /// </summary>
    Task SetACVoltageModeAsync();

    /// <summary>
    /// Измеряет переменное напряжение.
    /// </summary>
    Task<double> MeasureACVoltageAsync();
  }
}
