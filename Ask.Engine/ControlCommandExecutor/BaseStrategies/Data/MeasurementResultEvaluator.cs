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
  /// <param name="random">Необязательный генератор случайного направления для тестирования.</param>
  /// <returns>Признак успешного измерения и итоговое измеренное значение.</returns>
  internal static (bool IsSuccessful, double Value) Evaluate(
    MeasurementRange measurementRange,
    bool isOverloadExpected = false,
    Random? random = null)
  {
    ArgumentNullException.ThrowIfNull(measurementRange);

    double value = measurementRange.TargetValue;
    bool isSimulationEnabled = ExecutionConfig.GetIsIdleModeEnabled()
      && ExecutionConfig.GetIsErrorSimulationEnabled();

    if (isSimulationEnabled)
    {
      value = IdleMeasurementErrorSimulator.CreateValue(
        measurementRange.LowerBound,
        measurementRange.UpperBound,
        random);
    }

    bool isSuccessful = !isSimulationEnabled && (isOverloadExpected
      ? MeasurementValueFormatter.IsOverloadValue(value)
      : measurementRange.UpperBound != -1
        ? value >= measurementRange.LowerBound && value <= measurementRange.UpperBound
        : value >= measurementRange.LowerBound);

    return (isSuccessful, value);
  }
}
