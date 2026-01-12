namespace Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule.Capabilities
{
  /// <summary>
  /// Интерфейс для управления состоянием модуля релейного управления (МКР).
  /// </summary>
  public interface IStateManager
  {
    /// <summary>
    /// Инициализирует модуль коммутации реле.
    /// </summary>
    /// <returns>Кортеж (успешность подключения, ответ от устройства).</returns>
    Task<(bool Connect, string Answer)> Initialize();

    /// <summary>
    /// Выполняет сброс всех реле на МКР.
    /// </summary>
    /// <returns>Асинхронная операция.</returns>
    Task ResetAsync();
  }
}
