namespace Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter.Capabilities
{
  /// <summary>
  /// Интерфейс для управления соединением с мультиметром Keysight.
  /// </summary>
  public interface IConnection
  {
    /// <summary>
    /// Асинхронно устанавливает соединение с устройством.
    /// </summary>
    /// <returns>Задача, возвращающая true, если соединение успешно установлено.</returns>
    Task<bool> InitializeAsync();

    /// <summary>
    /// Асинхронно устанавливает соединение с устройством.
    /// </summary>
    /// <returns>Задача, возвращающая true, если соединение успешно установлено.</returns>
    Task<bool> ConnectAsync();

    /// <summary>
    /// Разрывает соединение с устройством.
    /// </summary>
    void Disconnect();
  }
}
