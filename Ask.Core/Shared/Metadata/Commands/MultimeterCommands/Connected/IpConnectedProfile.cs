using System.Net.Sockets;

namespace Ask.Core.Shared.Metadata.Commands.MultimeterCommands.Connected
{
  /// <summary>
  /// Профиль параметров подключения устройства по сети TCP/IP.
  /// </summary>
  public class IpConnectedProfile : ConnectedBaseProfile
  {
    /// <summary>
    /// Команда сброса устройства.
    /// </summary>
    public string Reset { get; set; } = string.Empty;

    /// <summary>
    /// Порт, используемый для связи с устройством.
    /// По умолчанию используется порт SCPI — 5025.
    /// </summary>
    public int Port { get; set; } = 5025;

    /// <summary>
    /// TCP-клиент, используемый для подключения к устройству.
    /// </summary>
    public TcpClient TcpClient { get; set; }

    /// <summary>
    /// Сетевой поток для обмена данными с устройством.
    /// </summary>
    public NetworkStream Stream { get; set; }
  }
}
