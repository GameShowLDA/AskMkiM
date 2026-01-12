namespace Ask.Core.Services.Errors.Device.DeviceBusCommutation
{
  /// <summary>
  /// Фабрика исключений, связанных с подключением и отключением резисторов.
  /// </summary>
  public static class ResistorExceptionFactory
  {
    /// <summary>
    /// Исключение при ошибке подключения резистора.
    /// </summary>
    public static DeviceException ConnectFailed(string number) =>
        new($"Ошибка подключения резистора №{number}");

    /// <summary>
    /// Исключение при ошибке отключения резистора.
    /// </summary>
    public static DeviceException DisconnectFailed(string number) =>
        new($"Ошибка отключения резистора №{number}");
  }
}
