namespace Ask.Device.ResponseProcessor.Multimeter.ResponseModels;

/// <summary>
/// Определяет состояние результата измерения мультиметра.
/// </summary>
public enum MeasurementState
{
  /// <summary>
  /// Получено числовое значение измерения.
  /// </summary>
  Value,

  /// <summary>
  /// Мультиметр сообщил о перегрузке измерительного диапазона.
  /// </summary>
  Overload
}
