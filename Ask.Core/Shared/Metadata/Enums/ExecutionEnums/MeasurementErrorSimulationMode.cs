namespace Ask.Core.Shared.Metadata.Enums.ExecutionEnums;

/// <summary>
/// Определяет направление отклонения при симуляции ошибки измерения.
/// </summary>
public enum MeasurementErrorSimulationMode
{
  /// <summary>
  /// Симуляция ошибок измерения отключена.
  /// </summary>
  None = 0,

  /// <summary>
  /// С равной вероятностью формируется значение выше или ниже нормы.
  /// </summary>
  Random = 1,

  /// <summary>
  /// Формируется значение выше верхней границы нормы.
  /// </summary>
  AboveNorm = 2,

  /// <summary>
  /// Формируется значение ниже нижней границы нормы.
  /// </summary>
  BelowNorm = 3,
}
