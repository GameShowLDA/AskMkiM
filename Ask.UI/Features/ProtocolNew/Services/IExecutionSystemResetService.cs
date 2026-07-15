namespace Ask.UI.Features.ProtocolNew.Services;

/// <summary>
/// Определяет операцию возврата аппаратуры и глобального состояния приложения в исходное состояние.
/// </summary>
internal interface IExecutionSystemResetService
{
  /// <summary>
  /// Асинхронно сбрасывает устройства и связанное состояние выполнения.
  /// </summary>
  Task ResetAsync();
}
