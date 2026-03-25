namespace Ask.Core.Shared.DTO.Settings;

/// <summary>
/// DTO настроек выполнения.
/// Определяет режимы и поведение процесса выполнения без привязки к источнику данных.
/// </summary>
public class SettingsExecutionDto
{
  /// <summary>
  /// Активирует холостой режим выполнения (без реальных воздействий).
  /// </summary>
  public bool IdleModeExecution { get; set; }

  /// <summary>
  /// Включает режим симуляции ошибок.
  /// </summary>
  public bool IsErrorSimulationMode { get; set; }

  /// <summary>
  /// Включает пошаговый режим выполнения.
  /// </summary>
  public bool StepByStepMode { get; set; }

  /// <summary>
  /// Останавливает выполнение при возникновении ошибки.
  /// </summary>
  public bool StopOnError { get; set; }
}