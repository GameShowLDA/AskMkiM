namespace Ask.Core.Shared.Metadata.Commands.MultimeterCommands.Connected
{
  /// <summary>
  /// Профиль параметров подключения устройства по USB.
  /// </summary>
  public class UsbConnectedProfile : ConnectedBaseProfile
  {
    /// <summary>
    /// Последний успешно определённый путь к USB-устройству.
    /// </summary>
    public string LastResolvedDevicePath { get; set; } = string.Empty;

    /// <summary>
    /// Шаблон поиска VISA-ресурса USB.
    /// </summary>
    public string VisaResourcePattern { get; set; } = "USB?*INSTR";

    /// <summary>
    /// Количество попыток открытия соединения.
    /// </summary>
    public int OpenRetryCount { get; set; } = 3;

    /// <summary>
    /// Задержка между попытками открытия соединения, в миллисекундах.
    /// </summary>
    public int OpenRetryDelayMs { get; set; } = 150;

    /// <summary>
    /// Размер буфера чтения в байтах.
    /// </summary>
    public int ReadBufferSize { get; set; } = 4096;

    /// <summary>
    /// Определяет, следует ли отправлять признак окончания передачи (END).
    /// </summary>
    public bool SendEndEnabled { get; set; } = true;

    /// <summary>
    /// Символ завершения сообщения.
    /// </summary>
    public byte TerminationCharacter { get; set; } = (byte)'\n';

    /// <summary>
    /// Определяет, используется ли символ завершения сообщения.
    /// </summary>
    public bool TerminationCharacterEnabled { get; set; } = true;

    /// <summary>
    /// Определяет, следует ли автоматически добавлять символ окончания строки к отправляемым командам.
    /// </summary>
    public bool AppendLineEnding { get; set; } = true;

    /// <summary>
    /// Определяет, следует ли использовать механизм ViewPower при работе с устройством.
    /// </summary>
    public bool UseViewPower { get; set; }
  }
}