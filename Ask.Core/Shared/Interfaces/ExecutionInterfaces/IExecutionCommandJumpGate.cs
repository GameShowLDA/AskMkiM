namespace Ask.Core.Shared.Interfaces.ExecutionInterfaces;

/// <summary>
/// Предоставляет исполнителю состояние паузы и сигнал перехода к другой команде.
/// </summary>
public interface IExecutionCommandJumpGate
{
  /// <summary>
  /// Признак приостановленного выполнения.
  /// </summary>
  bool IsExecutionPaused { get; }

  /// <summary>
  /// Прерывает ожидание паузы для обработки перехода к другой команде.
  /// </summary>
  void InterruptPauseForCommandJump();

  /// <summary>
  /// Прерывает текущую команду и возобновляет выполнение для перехода к следующей.
  /// </summary>
  void InterruptCurrentCommand();
}
