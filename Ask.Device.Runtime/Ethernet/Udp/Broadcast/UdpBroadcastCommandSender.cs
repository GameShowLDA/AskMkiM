using Ask.Device.Runtime.Commands;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Device.Runtime.Ethernet.Udp.Broadcast
{
  /// <summary>
  /// Отправляет широковещательные UDP-команды сетевым устройствам.
  /// </summary>
  public static class UdpBroadcastCommandSender
  {
    /// <summary>
    /// Базовый порт широковещательной отправки.
    /// </summary>
    private const int BroadcastPort = 8888;

    /// <summary>
    /// Широковещательный адрес стенда.
    /// </summary>
    private static readonly IPAddress BroadcastAddress = IPAddress.Parse("192.168.1.255");

    /// <summary>
    /// Общий сокет для широковещательной отправки команд.
    /// </summary>
    private static readonly Socket Socket = CreateBroadcastSocket();

    /// <summary>
    /// Выполняет ping устройства по указанному адресу.
    /// </summary>
    /// <param name="name">Имя устройства для логов.</param>
    /// <param name="ipAddress">IP-адрес устройства.</param>
    /// <returns><see langword="true"/>, если устройство отвечает на ping.</returns>
    public static async Task<bool> PingAsync(string name, IPAddress ipAddress)
    {
      try
      {
        using var ping = new Ping();
        PingReply reply = await ping.SendPingAsync(ipAddress, 10);
        bool success = reply.Status == IPStatus.Success;
        LogInformation(success ? $"{name}: Пинг успешен." : $"{name}: Пинг неудачен.", isDeviceLog: true);
        return success;
      }
      catch (Exception ex)
      {
        LogException("Ошибка пинга", ex, isDeviceLog: true);
        return false;
      }
    }

    /// <summary>
    /// Выполняет сброс всей сетевой аппаратуры широковещательной командой.
    /// </summary>
    public static async Task ResetAllDevicesAsync()
    {
      await SendBroadcastCommandAsync(new DeviceCommand(2, 0, 0, 0));
    }

    /// <summary>
    /// Отправляет команду широковещательно.
    /// </summary>
    /// <param name="command">Команда для отправки.</param>
    private static async Task SendBroadcastCommandAsync(DeviceCommand command)
    {
      try
      {
        var endPoint = new IPEndPoint(BroadcastAddress, BroadcastPort);
        byte[] sendBuffer = Encoding.ASCII.GetBytes(command.ToString());
        await Socket.SendToAsync(new ArraySegment<byte>(sendBuffer), SocketFlags.None, endPoint);

        LogInformation("Команда отправлена широковещательно.", isDeviceLog: true);
      }
      catch (SocketException ex)
      {
        LogException("Ошибка UDP-соединения", ex, isDeviceLog: true);
      }
      catch (TimeoutException ex)
      {
        LogException("Превышено время ожидания при широковещательной отправке", ex, isDeviceLog: true);
      }
      catch (ArgumentException ex)
      {
        LogException("Переданы неверные аргументы для широковещательной отправки", ex, isDeviceLog: true);
      }
      catch (Exception ex)
      {
        LogException("Непредвиденная ошибка при широковещательной отправке", ex, isDeviceLog: true);
        throw;
      }
    }

    /// <summary>
    /// Возвращает последний октет IPv4-адреса.
    /// </summary>
    /// <param name="ip">IPv4-адрес.</param>
    /// <returns>Числовое значение последнего октета.</returns>
    /// <exception cref="ArgumentException">Выбрасывается, если адрес не является IPv4.</exception>
    public static int GetLastOctet(IPAddress ip)
    {
      string ipString = ip.ToString();
      string[] parts = ipString.Split('.');

      if (parts.Length == 4 && int.TryParse(parts[3], out int lastOctet))
      {
        return lastOctet;
      }

      throw new ArgumentException("Адрес не является корректным IPv4-адресом.");
    }

    /// <summary>
    /// Создаёт сокет, готовый к широковещательной отправке.
    /// </summary>
    /// <returns>Настроенный UDP-сокет.</returns>
    private static Socket CreateBroadcastSocket()
    {
      var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
      socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, true);
      return socket;
    }
  }
}
