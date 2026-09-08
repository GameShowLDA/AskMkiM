namespace Ask.Core.Shared.Metadata.Enums.DeviceEnums;

/// <summary>
/// Определяет статус измерения пробойной установки.
/// </summary>
public enum BreakdownMeasurementStatus
{
  /// <summary>
  /// Измерение выполняется.
  /// </summary>
  Test,

  /// <summary>
  /// Измерение завершилось с браком.
  /// </summary>
  Fail,

  /// <summary>
  /// Измерение завершилось успешно.
  /// </summary>
  Pass
}
