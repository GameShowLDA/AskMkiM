namespace Ask.Core.Shared.Metadata.Commands.MultimeterCommands.Connected
{
  /// <summary>
  /// Базовый профиль команд подключения измерительного устройства.
  /// </summary>
  public class ConnectedBaseProfile
  {
    /// <summary>
    /// Команда инициализации или идентификации устройства.
    /// </summary>
    public string Initialize { get; set; } = "*IDN?";

    /// <summary>
    /// Команда проверки текущего режима работы устройства.
    /// </summary>
    public string CheckMode { get; set; }

    /// <summary>
    /// Время ожидания ответа устройства, в миллисекундах.
    /// </summary>
    public int Timeout { get; set; } = 1000;

    /// <summary>
    /// Команды отключения звуковой сигнализации после первой успешной инициализации.
    /// </summary>
    public IReadOnlyList<string> InitialBeeperDisableCommands { get; set; } = [];
  }
}
