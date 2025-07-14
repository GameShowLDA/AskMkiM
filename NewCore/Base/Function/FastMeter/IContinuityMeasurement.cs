namespace NewCore.Base.Function.FastMeter
{
  /// <summary>
  /// Интерфейс для проверки проводимости (прозвонки).
  /// </summary>
  public interface IContinuityMeasurement
  {
    /// <summary>
    /// Устанавливает режим прозвонки.
    /// </summary>
    Task<bool> SetContinuityModeAsync();

    /// <summary>
    /// Проверяет наличие проводимости.
    /// </summary>
    Task<bool> CheckContinuityAsync(bool expectedOutcome);
  }
}
