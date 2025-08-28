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
      OperationLock = new SemaphoreSlim(1, 1);
    }

    public SemaphoreSlim OperationLock { get; set; }

    /// <inheritdoc />
    public async Task<string> QueryAsync(string command, double responseDelay = 0, int timeout = 0, int port = 0, int delayBeforeCall = 0, CancellationToken cancellationToken = new CancellationToken())
    {
      try
      {
        int lastOctet = GetLastOctet(_device.IPAddress);
        int inputPort = port == 0 ? BaseInputPort + lastOctet : port;
        int outputPort = port == 0 ? BaseOutputPort + lastOctet : port;

        IPEndPoint deviceEndpoint = new IPEndPoint(_device.IPAddress, outputPort);
        using UdpClient udpClient = new UdpClient(new IPEndPoint(IPAddress.Any, inputPort));

        byte[] buffer = Encoding.UTF8.GetBytes(command);

        await udpClient.SendAsync(buffer, buffer.Length, deviceEndpoint);
        LogInformation($"[{_device.Name}] Отправка команды: \"{command}\" на {deviceEndpoint}", isDeviceLog: true);

        if (responseDelay > 0)
        {
          int roundedDelay = (int)Math.Ceiling(responseDelay);
          await Task.Delay(roundedDelay);
        }

        if (timeout > 0)
        {
          using var cts = new CancellationTokenSource(timeout);

          try
          {
            var receiveTask = udpClient.ReceiveAsync();
            var delayTask = Task.Delay(Timeout.Infinite, cts.Token);

            var completedTask = await Task.WhenAny(receiveTask, delayTask);

            if (completedTask == receiveTask)
            {
              UdpReceiveResult result = await receiveTask;
              string response = Encoding.UTF8.GetString(result.Buffer);
              LogInformation($"[{_device.Name}] Ответ от устройства: {response}", isDeviceLog: true);
              return response;
            }
            else
            {
              return LogWarning($"[{_device.Name}] Устройство не ответило в течение {timeout / 1000.0} секунд(ы).", isDeviceLog: true);
            }
          }
          catch (Exception ex)
          {
            LogException($"[{_device.Name}] Исключение при получении ответа", ex, isDeviceLog: true);
            return $"[{_device.Name}] Ошибка при получении ответа: {ex.Message}";
          }
        }

        return string.Empty;
      }
      catch (Exception ex)
      {
        LogException($"[{_device.Name}] Общая ошибка QueryAsync", ex, isDeviceLog: true);
        return $"[{_device.Name}] Общая ошибка QueryAsync: {ex.Message}";
      }
      finally
      { 
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
