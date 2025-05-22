using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using static Utilities.LoggerUtility;

namespace NewCore.Communication
{
  /// <summary>
  /// Статический класс для отправки команд устройствам.
  /// </summary>
  static public class DeviceCommandSender
  {

    /// <summary>
    /// Порт для отправки сообщений.
    /// </summary>
    private static readonly int _portOutput = 8888;

    /// <summary>
    /// Общий сокет для отправки сообщений.
    /// </summary>
    private static readonly Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

    /// <summary>
    /// Проверка IP на подключение к сети.
    /// </summary>
    /// <param name="name">Имя устройства.</param>
    /// <param name="ipAddress">Ip адрес.</param>
    /// <returns>Возвращает успешное подключение или нет.</returns>
    public static async Task<bool> PingAsync(string name, IPAddress ipAddress)
    {
      try
      {
        using (Ping ping = new Ping())
        {
          PingReply reply = await ping.SendPingAsync(ipAddress, 10);
          bool success = reply.Status == IPStatus.Success;
          LogInformation(success ? $"{name}: Пинг успешен." : $"{name}: Пинг неудачен.");
          return success;
        }
      }
      catch (Exception ex)
      {
        LogException($"Ошибка пинга", ex);
        return false;
      }
    }

    /// <summary>
    /// Выполняет сброс всей аппаратуры.
    /// </summary>
    /// <returns></returns>
    public static async Task ResetAllSystem()
    {
      await SendBroadcastCommandAsync(new DeviceCommand(2, 0, 0, 0));
    }

    /// <summary>
    /// Отправляет сообщение широковещательно.
    /// </summary>
    /// <param name="command">Команда для отправки.</param>
    /// <returns></returns>
    private static async Task SendBroadcastCommandAsync(DeviceCommand command)
    {
      try
      {
        // Широковещательный адрес для локальной сети
        IPAddress broadcastAddress = IPAddress.Parse("255.255.255.255");
        IPEndPoint ep = new IPEndPoint(broadcastAddress, _portOutput);

        // Делаем сокет способным отправлять широковещательные сообщения
        socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, true);

        byte[] sendBuf = Encoding.UTF8.GetBytes(command.ToString());

        // Отправляем широковещательное сообщение
        await socket.SendToAsync(new ArraySegment<byte>(sendBuf), SocketFlags.None, ep).ConfigureAwait(false);

        LogInformation("Команда отправлена широковещательно.");
      }
      catch (SocketException ex)
      {
        LogException($"Ошибка соединения", ex);
      }
      catch (TimeoutException ex)
      {
        LogException($"Превышено время ожидания", ex);
      }
      catch (ArgumentException ex)
      {
        LogException($"Неверные аргументы", ex);
      }
      catch (Exception ex)
      {
        LogException($"Непредвиденная ошибка", ex);
        throw;
      }
    }

    /// <summary>
    /// Возвращает последний октет из IPv4-адреса (например, для "192.168.1.10" вернёт 10).
    /// </summary>
    /// <param name="ip">IPv4-адрес в формате <see cref="IPAddress"/>.</param>
    /// <returns>Числовое значение последнего октета.</returns>
    /// <exception cref="ArgumentException">Выбрасывается, если адрес не является IPv4.</exception>
    public static int GetLastOctet(IPAddress ip)
    {
      // Преобразуем IP-адрес в строку и разбиваем по точкам
      string ipString = ip.ToString();
      string[] parts = ipString.Split('.');

      // Если частей ровно 4, считаем, что это IPv4
      if (parts.Length == 4)
      {
        // Преобразуем последний элемент в int
        if (int.TryParse(parts[3], out int lastOctet))
        {
          return lastOctet;
        }
        else
        {
          throw new ArgumentException("Последний октет не удалось преобразовать в число.");
        }
      }
      else
      {
        throw new ArgumentException("Адрес не является IPv4-адресом.");
      }
    }
  }
}
