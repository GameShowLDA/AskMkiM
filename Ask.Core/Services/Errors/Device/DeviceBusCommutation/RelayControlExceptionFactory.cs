namespace Ask.Core.Services.Errors.Device.DeviceBusCommutation
{
  /// <summary>
  /// Фабрика исключений для ошибок подключения и отключения реле устройства коммутации шин.
  /// </summary>
  public static class RelayControlExceptionFactory
  {
    /// <summary>
    /// Исключение при ошибке подключения реле.
    /// </summary>
    public static DeviceException ConnectFailed(int number) =>
        new($"Ошибка подключения реле №{number}");

    /// <summary>
    /// Исключение при ошибке отключения реле.
    /// </summary>
    public static DeviceException DisconnectFailed(int number) =>
        new($"Ошибка отключения реле №{number}");

    /// <summary>
    /// Исключение при ошибке включения реле.
    /// </summary>
    public static DeviceException EnableFailed() =>
        new("Ошибка включения реле");

    /// <summary>
    /// Исключение при ошибке отключения реле.
    /// </summary>
    public static DeviceException DisableFailed() =>
        new("Ошибка отключения реле");

    /// <summary>
    /// Исключение при ошибке подключения RC реле.
    /// </summary>
    public static DeviceException ConnectRCFailed() =>
        new("Ошибка подключения RC реле");

    /// <summary>
    /// Исключение при ошибке отключения RC реле.
    /// </summary>
    public static DeviceException DisconnectRCFailed() =>
        new("Ошибка отключения RC реле");
  }
}
