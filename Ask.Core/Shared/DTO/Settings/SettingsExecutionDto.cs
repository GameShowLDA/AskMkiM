using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ask.Core.Shared.DTO.Settings;

/// <summary>
/// DTO настроек выполнения.
/// Определяет режимы и поведение процесса выполнения без привязки к источнику данных.
/// </summary>
[Table("Execution")]
public class SettingsExecutionDto
{
  /// <summary>
  /// Идентификатор записи настроек.
  /// </summary>
  [Key]
  public int Id { get; set; }

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

  /// <summary>
  /// Включает режим совместимости со старой системой АСК-МКИ.
  /// Использует таблицу соответствия модулей МКР-350 и разъёмов переходной панели.
  /// </summary>
  public bool LegacyCompatibilityMode { get; set; }
}
