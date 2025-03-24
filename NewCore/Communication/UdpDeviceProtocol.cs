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
    private static readonly Socket SharedSocket;
    private readonly DeviceWithIP _device;

    static UdpDeviceProtocol()
    {
      SharedSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
      SharedSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
    }

    /// <summary>
    /// Инициализирует новый экземпляр <see cref="UdpDeviceProtocol"/> для указанного IP-устройства.
    /// </summary>
    /// <param name="device">Устройство с IP-подключением.</param>
    public UdpDeviceProtocol(DeviceWithIP device)
    {
      _device = device ?? throw new ArgumentNullException(nameof(device));
    }

    /// <inheritdoc />
    public async Task<string> QueryAsync(string command, int timeout = 0, int responseDelay = 0)
    {
      try
      {
        int lastOctet = GetLastOctet(_device.IPAddress);
        int inputPort = BaseInputPort + lastOctet;
        int outputPort = BaseOutputPort + lastOctet;
        IPEndPoint deviceEndpoint = new IPEndPoint(_device.IPAddress, outputPort);

        using UdpClient udpClient = new UdpClient(new IPEndPoint(IPAddress.Any, inputPort));

        // Гарантированно слушаем ДО отправки
        Task<UdpReceiveResult> receiveTask = udpClient.ReceiveAsync();

        byte[] buffer = Encoding.UTF8.GetBytes(command);
        await udpClient.SendAsync(buffer, buffer.Length, deviceEndpoint);
        LogInformation($"[{_device.Name}] Отправка команды: \"{command}\" на {deviceEndpoint}");

        if (responseDelay > 0)
        {
          await Task.Delay(responseDelay);
        }

        if (timeout > 0)
        {
          Task timeoutTask = Task.Delay(timeout);
          if (await Task.WhenAny(receiveTask, timeoutTask) == receiveTask)
          {
            string response = Encoding.UTF8.GetString((await receiveTask).Buffer);
            LogInformation($"[{_device.Name}] Ответ от устройства: {response}");
            return response;
          }
          else
          {
            return LogWarning($"[{_device.Name}] Устройство не ответило в течение {timeout / 1000.0} секунд(ы).");
          }
        }

        return string.Empty;
      }
      catch (Exception ex)
      {
        return LogError($"[{_device.Name}] Ошибка при отправке/приёме: {ex.Message}");
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

      using (UdpClient receiver = new UdpClient(new IPEndPoint(IPAddress.Any, inputPort)))
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
            return LogWarning($"[{_device.Name}] Устройство не ответило в течение {timeout / 1000.0} секунд(ы).");
          }
        }
        catch (Exception ex)
        {
          return LogError($"[{_device.Name}] Ошибка при получении ответа: {ex.Message}");
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
