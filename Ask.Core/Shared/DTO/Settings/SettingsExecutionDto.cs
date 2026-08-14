using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;

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
  /// Задаёт режим симуляции ошибочных результатов измерений.
  /// </summary>
  [Column("IsErrorSimulationMode")]
  public TypeErroneousMeasurement ErroneousMeasurementType { get; set; }

  /// <summary>
  /// Включает симуляцию аппаратных ошибок оборудования.
  /// </summary>
  public bool IsHardwareErrorSimulationMode { get; set; }

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

  /// <summary>
  /// Отключает проверку состояния питания перед запуском выполнения.
  /// </summary>
  public bool DisablePowerCheck { get; set; }
}
