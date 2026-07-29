namespace Ask.Core.Shared.DTO.Devices.Measurements
{
  /// <summary>
  /// Содержит заданное значение и допустимый диапазон измерения.
  /// </summary>
  public class MeasurementRange
  {
    /// <summary>
    /// Создаёт параметры измерения с заданным значением и допустимыми границами.
    /// </summary>
    /// <param name="targetValue">Заданное значение измеряемой величины.</param>
    /// <param name="lowerBound">Нижняя граница допустимого диапазона.</param>
    /// <param name="upperBound">Верхняя граница допустимого диапазона.</param>
    public MeasurementRange(double targetValue, double lowerBound, double upperBound)
    {
      TargetValue = targetValue;
      LowerBound = lowerBound;
      UpperBound = upperBound;
    }

    /// <summary>
    /// Заданное значение измеряемой величины.
    /// </summary>
    public double TargetValue { get; set; }

    /// <summary>
    /// Нижняя граница допустимого диапазона.
    /// </summary>
    public double LowerBound { get; }

    /// <summary>
    /// Верхняя граница допустимого диапазона.
    /// </summary>
    public double UpperBound { get; }
  }
}
