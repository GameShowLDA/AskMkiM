namespace Ask.Core.Shared.Interfaces.DeviceInterfaces.PowerSourceModule.Capabilities
{
  /// <summary>
  /// Интерфейс для управления состоянием модуля источника напряжения/тока.
  /// </summary>
  public interface IStateManager
  {
    /// <summary>
    /// Выполняет сброс состояния устройства.
    /// </summary>
    /// <returns>Задача, содержащая результат операции (true, если успешно).</returns>
    Task<bool> ResetAsync();

    /// <summary>
    /// Инициализирует устройство и проверяет соединение.
    /// </summary>
    /// <returns>Задача, содержащая кортеж, где:
    /// <para>- <c>Connect</c>: true, если соединение успешно.</para>
    /// <para>- <c>Answer</c>: строка с ответом от устройства.</para>
    /// </returns>
    Task<(bool Connect, string Answer)> Initialize();
  }
}
