namespace Ask.Device.ResponseProcessor.Multimeter.ResponseModels;

/// <summary>
/// Содержит числовое значение, полученное из ответа мультиметра.
/// </summary>
public sealed class MeasurementResponse
{
  /// <summary>
  /// Исходная строка ответа прибора.
  /// </summary>
  public required string RawValue { get; init; }

  /// <summary>
  /// Числовое значение ответа.
  /// </summary>
  public double Value { get; init; }

  /// <summary>
  /// Состояние результата измерения.
  /// </summary>
  public MeasurementState State { get; init; }
}
