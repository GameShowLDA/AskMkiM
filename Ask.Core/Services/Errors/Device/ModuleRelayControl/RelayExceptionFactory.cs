namespace Ask.Core.Services.Errors.Device.ModuleRelayControl
{
  /// <summary>
  /// Фабрика исключений для ошибок подключения и отключения реле (точек).
  /// </summary>
  public static class RelayExceptionFactory
  {
    /// <summary>
    /// Ошибка подключения отдельной точки.
    /// </summary>
    public static DeviceException ConnectPointFailed(string description) =>
        new($"Ошибка подключения точки {description}");

    /// <summary>
    /// Ошибка подключения отдельной точки.
    /// </summary>
    public static DeviceException ConnectPointFailed(string point, string name, int chassis, int number) =>
        new($"Ошибка подключения точки {point} на {name}({chassis}.{number})");

    /// <summary>
    /// Ошибка отключения отдельной точки.
    /// </summary>
    public static DeviceException DisconnectPointFailed(string description) =>
        new($"Ошибка отключения точки {description}");


    /// <summary>
    /// Ошибка подключения отдельной точки.
    /// </summary>
    public static DeviceException DisconnectPointFailed(string point, string name, int chassis, int number) =>
        new($"Ошибка отключения точки {point} на {name}({chassis}.{number})");

    /// <summary>
    /// Ошибка подключения диапазона точек.
    /// </summary>
    public static DeviceException ConnectRangeFailed(string description) =>
        new($"Ошибка подключения диапазона точек {description}");

    /// <summary>
    /// Ошибка отключения диапазона точек.
    /// </summary>
    public static DeviceException DisconnectRangeFailed(string description) =>
        new($"Ошибка отключения диапазона точек {description}");

    /// <summary>
    /// Ошибка отключения диапазона точек.
    /// </summary>
    public static DeviceException ConnectingPointToNewBusFailed(string description) =>
        new($"Ошибка переподключения точки {description}");


    /// <summary>
    /// Ошибка отключения отдельной точки.
    /// </summary>
    public static DeviceException DisconnectAllPointFailed() =>
        new($"Ошибка отключения всех точек ");
  }
}
