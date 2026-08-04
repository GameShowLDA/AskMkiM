using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.DTO.Devices.Measurements;
using Ask.Core.Shared.Metadata.Static.Messages;

namespace Ask.Engine.ControlCommandExecutor.BaseStrategies.Data;

/// <summary>
/// Проверяет измеренное значение на соответствие допустимому диапазону.
/// </summary>
internal static class MeasurementResultEvaluator
{
  /// <summary>
  /// Определяет итоговое значение измерения и проверяет его допустимость.
  /// </summary>
  /// <param name="measurementRange">Измеренное значение и границы допустимого диапазона.</param>
  /// <param name="isOverloadExpected">Признак ожидаемой перегрузки прибора.</param>
  /// <returns>Признак успешного измерения и итоговое измеренное значение.</returns>
  internal static (bool IsSuccessful, double Value) Evaluate(
    MeasurementRange measurementRange,
    bool isOverloadExpected = false)
  {
    ArgumentNullException.ThrowIfNull(measurementRange);

    double value = measurementRange.TargetValue;
    if (ExecutionConfig.GetIsIdleModeEnabled() && ExecutionConfig.GetIsErrorSimulationEnabled())
    {
      var random = new Random();
      value = measurementRange.UpperBound != -1
        ? random.NextDouble() * ((measurementRange.UpperBound + 1) * 2)
        : random.NextDouble();
    }

    bool isSuccessful = isOverloadExpected
      ? MeasurementValueFormatter.IsOverloadValue(value)
      : measurementRange.UpperBound != -1
        ? value >= measurementRange.LowerBound && value <= measurementRange.UpperBound
        : value >= measurementRange.LowerBound;

    return (isSuccessful, value);
  }
}
