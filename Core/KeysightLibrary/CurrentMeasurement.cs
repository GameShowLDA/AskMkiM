namespace Core.KeysightLibrary
{
  /// <summary>
  /// Класс для взаимодействия с током.
  /// </summary>
  public class CurrentMeasurement
  {
    /// <summary>
    /// Измеряет постоянный ток.
    /// </summary>
    /// <returns>Измеренное значение постоянного тока.</returns>
    public double MeasureCurrentDC()
    {
      // Код реализации здесь
      return 0.0;
    }

    /// <summary>
    /// Устанавливает разрешение для измерения постоянного тока.
    /// </summary>
    /// <param name="resolution">Значение разрешения.</param>
    public void SetCurrentDCResolution(double resolution)
    {
      // Код реализации здесь
    }

    /// <summary>
    /// Устанавливает вторичный параметр для измерения постоянного тока.
    /// </summary>
    /// <param name="secondary">Вторичный параметр.</param>
    public void SetCurrentDCSecondary(double secondary)
    {
      // Код реализации здесь
    }

    /// <summary>
    /// Устанавливает диапазон для измерения постоянного тока.
    /// </summary>
    /// <param name="range">Диапазон измерения.</param>
    public void SetCurrentDCRange(double range)
    {
      // Код реализации здесь
    }

    /// <summary>
    /// Измеряет переменный ток.
    /// </summary>
    /// <returns>Измеренное значение переменного тока.</returns>
    public double MeasureCurrentAC()
    {
      // Код реализации здесь
      return 0.0;
    }

    /// <summary>
    /// Устанавливает вторичный параметр для измерения переменного тока.
    /// </summary>
    /// <param name="secondary">Вторичный параметр.</param>
    public void SetCurrentACSecondary(double secondary)
    {
      // Код реализации здесь
    }

    /// <summary>
    /// Устанавливает диапазон для измерения переменного тока.
    /// </summary>
    /// <param name="range">Диапазон измерения.</param>
    public void SetCurrentACRange(double range)
    {
      // Код реализации здесь
    }
  }
}
