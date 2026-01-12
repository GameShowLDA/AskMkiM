namespace Ask.Core.Services.Errors.Device.DeviceBusCommutation
{
  /// <summary>
  /// Фабрика исключений, связанных с подключением и отключением конденсаторов.
  /// </summary>
  public static class CapacitorExceptionFactory
  {
    /// <summary>
    /// Исключение при ошибке подключения конденсатора.
    /// </summary>
    public static DeviceException ConnectFailed(string number) =>
        new($"Ошибка подключения конденсатора [{number}]");

    /// <summary>
    /// Исключение при ошибке отключения конденсатора.
    /// </summary>
    public static DeviceException DisconnectFailed(string number) =>
        new($"Ошибка отключения конденсатора [{number}]");
  }
}
