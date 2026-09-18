using Ask.Core.Shared.Metadata.Enums.DeviceEnums;

namespace Ask.Core.Services.Config.AppSettings;

/// <summary>
/// Формирует ошибочные результаты измерений для холостого режима.
/// </summary>
public static class IdleMeasurementErrorSimulator
{
  private const double UndefinedUpperBound = -1;

  /// <summary>
  /// Пытается сформировать значение в соответствии с текущим режимом симуляции.
  /// </summary>
  /// <param name="lowerBound">
  /// Нижняя граница допустимого диапазона.
  /// </param>
  /// <param name="upperBound">
  /// Верхняя граница допустимого диапазона или <c>-1</c>, если верхняя граница не задана.
  /// </param>
  /// <param name="value">Сформированное ошибочное значение.</param>
  /// <returns>
  /// <see langword="true"/>, если симуляция включена и значение сформировано;
  /// иначе <see langword="false"/>.
  /// </returns>
  public static bool TryGetValue(double lowerBound, double upperBound, out double value)
  {
    TypeErroneousMeasurement type = ExecutionConfig.GetErroneousMeasurementType();
    if (!ExecutionConfig.GetIsIdleModeEnabled() || type == TypeErroneousMeasurement.None)
    {
      value = default;
      return false;
    }

    value = GenerateValue(lowerBound, upperBound, type);
    return true;
  }

  /// <summary>
  /// Формирует ошибочное значение указанного типа.
  /// </summary>
  internal static double GenerateValue(
    double lowerBound,
    double upperBound,
    TypeErroneousMeasurement type)
  {
    double effectiveUpperBound = upperBound == UndefinedUpperBound
      ? CreateFallbackUpperBound(lowerBound)
      : upperBound;

    return type switch
    {
      TypeErroneousMeasurement.Low => GenerateBelow(lowerBound, effectiveUpperBound),
      TypeErroneousMeasurement.High => GenerateAbove(lowerBound, effectiveUpperBound),
      TypeErroneousMeasurement.Rnd => Random.Shared.Next(2) == 0
        ? GenerateBelow(lowerBound, effectiveUpperBound)
        : GenerateAbove(lowerBound, effectiveUpperBound),
      _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Неизвестный тип симуляции ошибки измерения."),
    };
  }

  private static double GenerateBelow(double lowerBound, double upperBound)
  {
    double value = lowerBound - GetDeviation(lowerBound, upperBound);
    return value < lowerBound ? value : Math.BitDecrement(lowerBound);
  }

  private static double GenerateAbove(double lowerBound, double upperBound)
  {
    double value = upperBound + GetDeviation(lowerBound, upperBound);
    return value > upperBound ? value : Math.BitIncrement(upperBound);
  }

  private static double GetDeviation(double lowerBound, double upperBound)
  {
    double magnitude = Math.Max(GetFiniteMagnitude(lowerBound), GetFiniteMagnitude(upperBound));
    double scale = Math.Max(1, magnitude);
    return scale * (0.01 + (Random.Shared.NextDouble() * 0.09));
  }

  private static double GetFiniteMagnitude(double value)
  {
    return double.IsFinite(value) ? Math.Abs(value) : 1;
  }

  private static double CreateFallbackUpperBound(double lowerBound)
  {
    double step = Math.Max(1, GetFiniteMagnitude(lowerBound));
    double upperBound = lowerBound + step;
    return upperBound > lowerBound ? upperBound : Math.BitIncrement(lowerBound);
  }
}
