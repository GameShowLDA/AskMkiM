using Ask.Core.Shared.Metadata.Enums.FileEnums;
using static Ask.Core.Shared.Metadata.Static.DelegateManager;

namespace Ask.Core.Shared.DTO.Executor
{
  /// <summary>
  /// Настройки выполнения действия.
  /// </summary>
  public sealed class ActionSettings
  {
    /// <summary>
    /// Делегат запуска.
    /// </summary>
    public required StartDelegate StartDelegate { get; init; }

    /// <summary>
    /// Делегат остановки.
    /// </summary>
    public StopDelegate? StopDelegate { get; init; }

    /// <summary>
    /// Делегат возврата.
    /// </summary>
    public ReturnDelegate? ReturnDelegate { get; init; }

    /// <summary>
    /// Делегат предварительных действий.
    /// </summary>
    public PreActionDelegate? PreActionDelegate { get; init; }

    /// <summary>
    /// Выполнять проверку питания.
    /// </summary>
    public bool CheckPower { get; init; } = true;

    /// <summary>
    /// Тип выполняемой проверки.
    /// </summary>
    public CheckType CheckType { get; init; } = CheckType.None;

    /// <summary>
    /// Автоматически добавлять сообщения об ошибках выполнения в итоговое заключение.
    /// </summary>
    public bool AccumulateErrorMessages { get; init; }

    /// <summary>
    /// Имя запускаемого процесса.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Время начала выполнения.
    /// </summary>
    public TimeOnly StartTime { get; set; }

    /// <summary>
    /// Время, затраченное на выполнение теста.
    /// </summary>
    public TimeSpan ExecutionDuration { get; set; }

    /// <summary>
    /// Ошибки, сформированные во время выполнения действия.
    /// </summary>
    public List<string> ExecutionErrors { get; } = new();
  }
}
