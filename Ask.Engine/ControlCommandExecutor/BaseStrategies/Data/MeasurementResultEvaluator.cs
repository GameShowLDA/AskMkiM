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
    if (IdleMeasurementErrorSimulator.TryGetValue(
          measurementRange.LowerBound,
          measurementRange.UpperBound,
          out double erroneousValue))
    {
      value = erroneousValue;
    }

    bool isOverload = MeasurementValueFormatter.IsOverloadValue(value);
    bool isSuccessful = isOverloadExpected
      ? isOverload
      : !isOverload && measurementRange.UpperBound != -1
        ? value >= measurementRange.LowerBound && value <= measurementRange.UpperBound
        : !isOverload && value >= measurementRange.LowerBound;

    return (isSuccessful, value);
  }

  /// <summary>
  /// Проверяет, подтверждает ли измерение разрыв цепи относительно заданного порога.
  /// </summary>
  /// <param name="value">Измеренное сопротивление.</param>
  /// <param name="disconnectionThreshold">Граница, выше которой цепь считается разобщённой.</param>
  /// <returns>Признак разобщения цепи и измеренное сопротивление.</returns>
  internal static (bool IsSuccessful, double Value) EvaluateDisconnection(
    double value,
    double disconnectionThreshold)
  {
    bool isSuccessful = MeasurementValueFormatter.IsOverloadValue(value)
      || value > disconnectionThreshold;

    return (isSuccessful, value);
  }
}
