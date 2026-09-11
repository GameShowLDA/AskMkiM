namespace Ask.Core.Shared.Metadata.Enums.DeviceEnums;

/// <summary>
/// Режим симуляции ошибочного результата измерения в холостом режиме.
/// </summary>
public enum TypeErroneousMeasurement
{
  /// <summary>
  /// Ошибки измерений не симулируются.
  /// </summary>
  None = 0,

  /// <summary>
  /// Результат случайно формируется ниже или выше допустимого диапазона.
  /// </summary>
  Rnd = 1,

  /// <summary>
  /// Результат всегда формируется ниже нижней границы допустимого диапазона.
  /// </summary>
  Low = 2,

  /// <summary>
  /// Результат всегда формируется выше верхней границы допустимого диапазона.
  /// </summary>
  High = 3,
}
