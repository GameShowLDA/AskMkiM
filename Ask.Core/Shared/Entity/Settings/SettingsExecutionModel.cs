using System.ComponentModel.DataAnnotations;

namespace Ask.Core.Shared.Entity.Settings
{
  /// <summary>
  /// Модель данных для выполнения.
  /// </summary>
  public class SettingsExecutionModel
  {
    /// <summary>
    /// Уникальный идентификатор модели.
    /// </summary>
    [Key]
    public int Id { get; set; } = 1;

    /// <summary>
    /// Указывает, активен ли холостой режим выполнения.
    /// </summary>
    public bool IdleModeExecution { get; set; }

    /// <summary>
    /// Указывает, активен ли режим симуляции ошибок.
    /// </summary>
    public bool IsErrorSimulationMode { get; set; }

    /// <summary>
    /// Указывает, активен ли пошаговый режим выполнения.
    /// </summary>
    public bool StepByStepMode { get; set; }

    /// <summary>
    /// Указывает, нужно ли останавливать выполнение при ошибке.
    /// </summary>
    public bool StopOnError { get; set; }
  }
}
