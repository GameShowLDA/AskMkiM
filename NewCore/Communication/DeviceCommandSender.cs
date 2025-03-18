using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using static Utilities.LoggerUtility;

namespace NewCore.Communication
{
  static public class DeviceCommandSender
  {
    /// <summary>
    /// Порт для входящих сообщений.
    /// </summary>
    private static readonly int _portInput = 8800;

    /// <summary>
    /// Порт для отправки сообщений.
    /// </summary>
    private static readonly int _portOutput = 8888;

    /// <summary>
    /// Общий сокет для отправки сообщений.
    /// </summary>
    private static readonly Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

    /// <summary>
    /// Отсылает команду по IP-адресу и, при необходимости, возвращает ответ от устройства.
    /// </summary>
    /// <param name="ip">IP-адрес целевого устройства.</param>
    /// <param name="command">Строка команды, которая будет отправлена на устройство.</param>
    /// <param name="timeout">Время ответа в миллисекундах.</param>
    /// <returns>Возвращает ответ от устройства, если <paramref name="awaitResponse"/> равно true; в противном случае возвращает пустую строку. В случае ошибки возвращает сообщение об ошибке.</returns>
    public static async Task<string> SendCommandAsync(IPAddress ip, DeviceCommand command, int timeout = 0)
    {
      int lastNumber = GetLastOctet(ip);
      try
      {
        IPEndPoint endPoint = new IPEndPoint(ip, _portOutput + lastNumber);
        LogInformation($"Отправка команды на {endPoint}");
        byte[] messageBuffer = Encoding.UTF8.GetBytes(command.ToString());
        await socket.SendToAsync(new ArraySegment<byte>(messageBuffer), SocketFlags.None, endPoint);


        return timeout > 0 ? await GetMessageDeviceAsync(timeout, lastNumber) : string.Empty;
      }
      catch (SocketException ex)
      {
        return LogError($"Ошибка соединения: {ex.Message}");
      }
      catch (TimeoutException ex)
      {
        return LogError($"Превышено время ожидания: {ex.Message}");
      }
      catch (ArgumentException ex)
      {
        return LogError($"Неверные аргументы: {ex.Message}");
      }
      catch (Exception ex) // Если нужно перехватить все исключения
      {
        LogError($"Непредвиденная ошибка: {ex.Message}");
        throw;
      }
    }

    /// <summary>
    /// Возвращает ответ от устройства.
    /// </summary>
    /// <param name="timeout">Время в миллисекундах.</param>
    /// <returns>Возвращает ответ от устройства.</returns>
    private static async Task<string> GetMessageDeviceAsync(int timeout, int lastNumber)
    {
      int portInput = _portInput + lastNumber;

      using (UdpClient responseClient = new UdpClient(portInput))
      {
        try
        {
          var receiveTask = responseClient.ReceiveAsync();
          var timeoutTask = Task.Delay(timeout);

          if (await Task.WhenAny(receiveTask, timeoutTask) == receiveTask)
          {
            string response = Encoding.UTF8.GetString((await receiveTask).Buffer);
            LogInformation($"Ответ от устройства: {response}");
            return response;
          }
          else
          {
            return LogWarning($"Устройство не ответило в течение {timeout / 1000.0} секунд(ы).");
          }
        }
        catch (SocketException ex)
        {
          return LogError($"Произошла ошибка при получении ответа от устройства: {ex.Message}");
        }
      }
    }

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
        LogError($"Ошибка пинга: {ex.Message}");
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
    /// <param name="command"></param>
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
        LogError($"Ошибка соединения: {ex.Message}");
      }
      catch (TimeoutException ex)
      {
        LogError($"Превышено время ожидания: {ex.Message}");
      }
      catch (ArgumentException ex)
      {
        LogError($"Неверные аргументы: {ex.Message}");
      }
      catch (Exception ex)
      {
        LogError($"Непредвиденная ошибка: {ex.Message}");
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
