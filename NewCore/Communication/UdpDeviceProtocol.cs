using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using NewCore.Base.Device;
using static Utilities.LoggerUtility;

namespace NewCore.Communication
{
  /// <summary>
  /// Реализация <see cref="IDeviceProtocol"/> для общения с устройствами по UDP-протоколу.
  /// </summary>
  public class UdpDeviceProtocol : IDeviceProtocol
  {
    private const int BaseOutputPort = 8888;
    private const int BaseInputPort = 8800;

    private readonly Socket _socket;
    private readonly DeviceWithIP _device;

    /// <summary>
    /// Инициализирует новый экземпляр <see cref="UdpDeviceProtocol"/> для указанного IP-устройства.
    /// </summary>
    /// <param name="device">Устройство с IP-подключением.</param>
    public UdpDeviceProtocol(DeviceWithIP device)
    {
      _device = device ?? throw new ArgumentNullException(nameof(device));
      _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
    }

    /// <inheritdoc />
    public async Task<string> QueryAsync(string command, int timeout = 0, int responseDelay = 0)
    {
      try
      {
        if (!IPAddress.TryParse(_device.ConnectionDetails, out IPAddress ip))
        {
          throw new ArgumentException($"Неверный формат IP-адреса: {_device.ConnectionDetails}");
        }

        int lastOctet = GetLastOctet(ip);
        int outputPort = BaseOutputPort + lastOctet;
        IPEndPoint endPoint = new IPEndPoint(ip, outputPort);

        LogInformation($"[{_device.Name}] Отправка команды: \"{command}\" на {endPoint}");

        byte[] buffer = Encoding.UTF8.GetBytes(command);
        await _socket.SendToAsync(new ArraySegment<byte>(buffer), SocketFlags.None, endPoint);

        if (responseDelay > 0)
        {
          await Task.Delay(responseDelay);
        }

        if (timeout > 0)
        {
          return await ReceiveResponseAsync(lastOctet, timeout, command);
        }

        return string.Empty;
      }
      catch (Exception ex)
      {
        LogError($"[{_device.Name}] Ошибка при отправке команды: {ex.Message}");
        throw;
      }
    }

    /// <summary>
    /// Получает ответ от устройства по UDP.
    /// </summary>
    /// <param name="lastOctet">Последний октет IP-адреса, используется для вычисления порта.</param>
    /// <param name="timeout">Таймаут ожидания, мс.</param>
    /// <returns>Ответ от устройства.</returns>
    private async Task<string> ReceiveResponseAsync(int lastOctet, int timeout, string command)
    {
      int inputPort = BaseInputPort + lastOctet;
      LogInformation($"[{_device.Name}] Чтение ответа команды \"{command}\" с порта {inputPort}");
      using (UdpClient receiver = new UdpClient(inputPort))
      {
        try
        {
          var receiveTask = receiver.ReceiveAsync();
          var timeoutTask = Task.Delay(timeout);

          if (await Task.WhenAny(receiveTask, timeoutTask) == receiveTask)
          {
            string response = Encoding.UTF8.GetString((await receiveTask).Buffer);
            LogInformation($"[{_device.Name}] Ответ от устройства: {response}");
            return response;
          }
          else
          {
            return LogWarning($"Устройство не ответило в течение {timeout / 1000.0} секунд(ы).");
          }
        }
        catch (Exception ex)
        {
          return LogError($"Произошла ошибка при получении ответа от устройства: {ex.Message}");
        }
      }
    }

    /// <summary>
    /// Получает последний октет IP-адреса.
    /// </summary>
    /// <param name="ip">IP-адрес.</param>
    /// <returns>Целое значение последнего октета.</returns>
    private int GetLastOctet(IPAddress ip)
    {
      string[] parts = ip.ToString().Split('.');
      if (parts.Length != 4 || !int.TryParse(parts[3], out int last))
      {
        throw new ArgumentException("Некорректный IP-адрес для определения порта.");
      }

      return last;
    }
  }
}
