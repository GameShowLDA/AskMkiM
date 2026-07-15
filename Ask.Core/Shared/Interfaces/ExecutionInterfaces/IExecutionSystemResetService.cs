namespace Ask.Core.Shared.Interfaces.ExecutionInterfaces;

/// <summary>
/// Определяет операцию возврата аппаратуры и глобального состояния приложения в исходное состояние.
/// </summary>
public interface IExecutionSystemResetService
{
  /// <summary>
  /// Асинхронно сбрасывает устройства и связанное состояние выполнения.
  /// </summary>
  Task ResetAsync();
}
