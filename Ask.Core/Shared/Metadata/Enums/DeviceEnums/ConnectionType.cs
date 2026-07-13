namespace Ask.Core.Shared.Metadata.Enums.DeviceEnums
{
  /// <summary>
  /// Определяет тип транспортного соединения, используемого для обмена данными с устройством.
  /// </summary>
  public enum ConnectionType
  {
    /// <summary>
    /// Подключение по протоколу UDP через IP-сеть.
    /// Используется для передачи данных без установки соединения.
    /// </summary>
    IP_UDP,

    /// <summary>
    /// Подключение по протоколу TCP через IP-сеть.
    /// Используется для надёжного обмена данными с установлением соединения.
    /// </summary>
    IP_TCP,

    /// <summary>
    /// Подключение через последовательный COM-порт.
    /// </summary>
    COM,

    /// <summary>
    /// Подключение через интерфейс Universal Serial Bus (USB).
    /// </summary>
    USB,
  }
}