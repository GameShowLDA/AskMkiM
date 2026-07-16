namespace Ask.Core.Shared.Metadata.Commands.MultimeterCommands.Connected
{
  /// <summary>
  /// Профиль команд подключения для устройств с интерфейсом COM.
  /// </summary>
  public class ComConnectedProfile : ConnectedBaseProfile
  {
    /// <summary>
    /// Команда сброса устройства к состоянию по умолчанию.
    /// </summary>
    public string Reset { get; set; } = "*RST";

    /// <summary>
    /// Команда очистки состояния и очереди ошибок устройства.
    /// </summary>
    public string Clear { get; set; } = "*CLS";
  }
}