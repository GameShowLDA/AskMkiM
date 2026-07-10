using System.Net.Sockets;

namespace Ask.Core.Shared.Metadata.Commands.MultimeterCommands.Connected
{
  public class IpConnectedProfile : ConnectedBaseProfile
  {
    /// <summary>
    /// Порт, используемый для связи с устройством (по умолчанию 5025).
    /// </summary>
    public int Port { get; set; } = 5025;

    public TcpClient TcpClient { get; set; }

    public NetworkStream Stream { get; set; }

  }
}
