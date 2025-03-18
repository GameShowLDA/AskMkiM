namespace NewCore.Base.Function.FastMeter
{
  /// <summary>
  /// Интерфейс для измерения постоянного напряжения.
  /// </summary>
  public interface IDcVoltageMeasurement
  {
    /// <summary>
    /// Устанавливает режим измерения постоянного напряжения.
    /// </summary>
    Task SetDCVoltageModeAsync();

    /// <summary>
    /// Измеряет постоянное напряжение.
    /// </summary>
    Task<double> MeasureDCVoltageAsync();
  }
}
