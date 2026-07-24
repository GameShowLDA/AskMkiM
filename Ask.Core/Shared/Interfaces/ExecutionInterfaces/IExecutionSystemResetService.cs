namespace Ask.Core.Shared.Interfaces.ExecutionInterfaces;

/// <summary>
/// Определяет операцию возврата глобального состояния выполнения в исходное состояние.
/// </summary>
public interface IExecutionSystemResetService
{
  /// <summary>
  /// Сбрасывает глобальное состояние выполнения.
  /// </summary>
  Task ResetAsync();
}
