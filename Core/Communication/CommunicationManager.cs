using System.Globalization;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using static Utilities.LoggerUtility;

namespace Core.Communication
{
  static public class CommunicationManager
  {

    /// <summary>
    /// Порт для входящих сообщений.
    /// </summary>
    private static readonly string PortInput = "8800";

    /// <summary>
    /// Порт для отправки сообщений.
    /// </summary>
    private static readonly string PortOutput = "8888";
    private static readonly Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

    /// <summary>
    /// Отсылает команду по IP-адресу и, при необходимости, возвращает ответ от устройства.
    /// </summary>
    /// <param name="ip">IP-адрес целевого устройства.</param>
    /// <param name="command">Строка команды, которая будет отправлена на устройство.</param>
    /// <param name="time">Время ответа в миллисекундах.</param>
    /// <returns>Возвращает ответ от устройства, если <paramref name="awaitResponse"/> равно true; в противном случае возвращает пустую строку. В случае ошибки возвращает сообщение об ошибке.</returns>
    public static async Task<string> SendCommandAsync(IPAddress ip, Command command, int time = 0)
    {
      try
      {
        IPEndPoint endPoint = new IPEndPoint(ip, Convert.ToInt32(PortOutput));
        byte[] messageBuffer = Encoding.UTF8.GetBytes(command.ToString());
        await socket.SendToAsync(new ArraySegment<byte>(messageBuffer), SocketFlags.None, endPoint);


        if (time > 0)
        {
          return await GetMessageDeviceAsync(time);
        }
        else
        {
          return string.Empty;
        }
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
    /// <param name="time">Время в миллисекундах.</param>
    /// <returns>Возвращает ответ от устройства.</returns>
    private static async Task<string> GetMessageDeviceAsync(int time)
    {
      if (!int.TryParse(PortInput, out int port))
      {
        return LogError($"Неверный формат порта.");
      }

      using (UdpClient responseClient = new UdpClient(port))
      {
        try
        {
          Task<UdpReceiveResult> receiveTask = responseClient.ReceiveAsync();
          Task timeoutTask = Task.Delay(time);

          if (await Task.WhenAny(receiveTask, timeoutTask).ConfigureAwait(true) == receiveTask)
          {
            UdpReceiveResult result = await receiveTask.ConfigureAwait(true);
            string responseMessage = Encoding.UTF8.GetString(result.Buffer);
            LogInformation($"Ответ от устройства: {responseMessage}");
            return responseMessage;
          }
          else
          {
            return LogWarning($"Устройство не ответило в течение {time / 1000.0} секунд(ы).");
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
      bool error = false;
      int timeout = 10;

      LogInformation($"Проверка {name}: Pinging {ipAddress} with 32 bytes of data:");
      using (Ping ping = new Ping())
      {
        try
        {
          PingReply reply = await ping.SendPingAsync(ipAddress, timeout);
          if (reply.Status == IPStatus.Success)
          {
            LogInformation($"Запрос. Успешное подключение.");
            error = true;
          }
          else
          {
            LogError($"Запрос. Ошибка подключение: {reply.Status}.");
            error = false;
          }
        }
        catch (PingException ex)
        {
          LogError($"Запрос. Ошибка подключение: {ex.Message}.");
          error = false;
        }
      }

      Console.WriteLine();

      return error;
    }

    /// <summary>
    /// Выполняет сброс всей аппаратуры.
    /// </summary>
    /// <returns></returns>
    public static async Task ResetAllSystem()
    {
      await SendBroadcastCommandAsync(new Command(2, 0, 0, 0));
    }

    /// <summary>
    /// Отправляет сообщение широковещательно.
    /// </summary>
    /// <param name="command"></param>
    /// <returns></returns>
    private static async Task SendBroadcastCommandAsync(Command command)
    {
      try
      {
        if (!int.TryParse(PortOutput, NumberStyles.Integer, CultureInfo.InvariantCulture, out int port))
        {
          LogError("Некорректное значение порта.");
          return;
        }

        // Широковещательный адрес для локальной сети
        IPAddress broadcastAddress = IPAddress.Parse("255.255.255.255");
        IPEndPoint ep = new IPEndPoint(broadcastAddress, port);

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
  }
}
