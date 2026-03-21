namespace Ask.Device.Communication.Com
{
  /// <summary>
  /// Представляет DTO с настройками последовательного порта для сериализации в JSON.
  /// </summary>
  public class SerialPortSettingsDto
  {
    /// <summary>
    /// Получает или задаёт имя порта, например <c>COM1</c>.
    /// </summary>
    public string PortName { get; set; } = string.Empty;

    /// <summary>
    /// Получает или задаёт скорость передачи данных в бодах.
    /// </summary>
    public int BaudRate { get; set; }

    /// <summary>
    /// Получает или задаёт строковое представление режима чётности.
    /// </summary>
    public string Parity { get; set; } = string.Empty;

    /// <summary>
    /// Получает или задаёт количество бит данных.
    /// </summary>
    public int DataBits { get; set; }

    /// <summary>
    /// Получает или задаёт строковое представление количества стоп-бит.
    /// </summary>
    public string StopBits { get; set; } = string.Empty;

    /// <summary>
    /// Получает или задаёт строковое представление режима управления потоком.
    /// </summary>
    public string Handshake { get; set; } = string.Empty;

    /// <summary>
    /// Получает или задаёт имя кодировки порта.
    /// </summary>
    public string EncodingName { get; set; } = string.Empty;
  }
}
