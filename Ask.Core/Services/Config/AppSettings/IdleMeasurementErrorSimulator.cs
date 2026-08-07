using Ask.Core.Shared.Metadata.Enums.ExecutionEnums;

namespace Ask.Core.Services.Config.AppSettings;

/// <summary>
/// Формирует результаты измерений за пределами допустимой нормы в холостом режиме.
/// </summary>
public static class IdleMeasurementErrorSimulator
{
  private const double OverloadValue = 9.9E+37;

  /// <summary>
  /// Возвращает значение выше или ниже нормы согласно настройке выполнения.
  /// </summary>
  /// <param name="lowerBound">Нижняя граница нормы.</param>
  /// <param name="upperBound">
  /// Верхняя граница нормы либо <c>-1</c>, если задана только нижняя граница.
  /// </param>
  /// <param name="random">Необязательный генератор случайного направления.</param>
  public static double CreateValue(
    double lowerBound,
    double upperBound,
    Random? random = null)
  {
    bool simulateAboveNorm = ExecutionConfig.GetMeasurementErrorSimulationMode() switch
    {
      MeasurementErrorSimulationMode.AboveNorm => true,
      MeasurementErrorSimulationMode.BelowNorm => false,
      _ => (random ?? Random.Shared).Next(2) == 1,
    };

    double deviation = CalculateDeviation(lowerBound, upperBound);
    if (!simulateAboveNorm)
    {
      return MoveOutsideBoundary(lowerBound, deviation, above: false);
    }

    return upperBound == -1
      ? OverloadValue
      : MoveOutsideBoundary(upperBound, deviation, above: true);
  }

  private static double CalculateDeviation(double lowerBound, double upperBound)
  {
    double rangeWidth = upperBound == -1
      ? 0
      : Math.Abs(upperBound - lowerBound);
    double referenceValue = Math.Abs(lowerBound);

    if (upperBound != -1)
    {
      referenceValue = Math.Max(referenceValue, Math.Abs(upperBound));
    }

    double deviation = Math.Max(rangeWidth * 0.1, referenceValue * 0.1);
    return double.IsFinite(deviation) && deviation > 0 ? deviation : 1;
  }

  private static double MoveOutsideBoundary(double boundary, double deviation, bool above)
  {
    double value = above ? boundary + deviation : boundary - deviation;
    if (double.IsFinite(value) && value != boundary)
    {
      return value;
    }

    return above ? Math.BitIncrement(boundary) : Math.BitDecrement(boundary);
  }
}
