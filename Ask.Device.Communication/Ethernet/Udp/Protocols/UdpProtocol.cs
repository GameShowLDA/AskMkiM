using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Device.Communication.Common;
using System.Net;
using System.Net.Sockets;
using System.Text;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Device.Communication.Ethernet.Udp
{
  /// <summary>
  /// Реализует универсальный транспортный протокол обмена с устройствами по UDP.
  /// </summary>
  public class UdpProtocol : IDeviceProtocol
  {
    /// <summary>
    /// Базовый порт отправки команд.
    /// </summary>
    private const int BaseOutputPort = 8888;

    /// <summary>
    /// Базовый порт получения ответов.
    /// </summary>
    private const int BaseInputPort = 8800;

    /// <summary>
    /// Устройство, для которого выполняется обмен.
    /// </summary>
    private readonly IDevice _device;

    /// <summary>
    /// Получает или задаёт семафор, запрещающий параллельную отправку команд в одно устройство.
    /// </summary>
    public SemaphoreSlim OperationLock { get; set; }

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="UdpProtocol"/>.
    /// </summary>
    /// <param name="device">Устройство, использующее протокол.</param>
    public UdpProtocol(IDevice device)
    {
      _device = device ?? throw new ArgumentNullException(nameof(device));
      OperationLock = new SemaphoreSlim(1, 1);
    }

    /// <summary>
    /// Отправляет UDP-команду устройству и при необходимости ожидает ответ.
    /// </summary>
    /// <param name="command">Команда для отправки.</param>
    /// <param name="responseDelay">Задержка перед чтением ответа в миллисекундах.</param>
    /// <param name="timeout">Таймаут ожидания ответа в миллисекундах.</param>
    /// <param name="port">Пользовательский порт обмена. Если равен нулю, порт вычисляется по IP-адресу.</param>
    /// <param name="delayBeforeCall">Задержка перед отправкой команды в миллисекундах.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Ответ устройства либо пустая строка при отсутствии ответа.</returns>
    public async Task<string> QueryAsync(
      string command,
      double responseDelay = 0,
      int timeout = 0,
      int port = 0,
      int delayBeforeCall = 0,
      CancellationToken cancellationToken = default)
    {
      using (await OperationLock.LockAsync(cancellationToken))
      {
        try
        {
          if (delayBeforeCall > 0)
          {
            await Task.Delay(delayBeforeCall, cancellationToken).ConfigureAwait(false);
          }

          IPAddress ipAddress = ResolveIpAddress();
          int lastOctet = GetLastOctet(ipAddress);
          int inputPort = port == 0 ? BaseInputPort + lastOctet : port;
          int outputPort = port == 0 ? BaseOutputPort + lastOctet : port;

          var deviceEndpoint = new IPEndPoint(ipAddress, outputPort);
          using var udpClient = new UdpClient(new IPEndPoint(IPAddress.Any, inputPort));
          byte[] buffer = Encoding.UTF8.GetBytes(command);

          await udpClient.SendAsync(buffer, buffer.Length, deviceEndpoint).ConfigureAwait(false);
          LogInformation($"[{_device.Name}] Отправка команды: \"{command}\" на {deviceEndpoint}", isDeviceLog: true);

          if (responseDelay > 0)
          {
            await Task.Delay((int)Math.Ceiling(responseDelay), cancellationToken).ConfigureAwait(false);
          }

          if (timeout <= 0)
          {
            return string.Empty;
          }

          using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
          timeoutCts.CancelAfter(timeout);

          UdpReceiveResult result = await udpClient.ReceiveAsync(timeoutCts.Token).ConfigureAwait(false);
          string response = Encoding.UTF8.GetString(result.Buffer);
          LogInformation($"[{_device.Name}] Ответ от устройства: {response}", isDeviceLog: true);
          return response;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
          return LogWarning($"[{_device.Name}] Устройство не ответило в течение {timeout / 1000.0} секунд(ы).", isDeviceLog: true);
        }
        catch (Exception ex)
        {
          LogException($"[{_device.Name}] Ошибка UDP QueryAsync", ex, isDeviceLog: true);
          return $"[{_device.Name}] Ошибка UDP QueryAsync: {ex.Message}";
        }
      }
    }

    /// <summary>
    /// Возвращает IP-адрес устройства из параметров подключения.
    /// </summary>
    /// <returns>IP-адрес устройства.</returns>
    /// <exception cref="InvalidOperationException">Выбрасывается, если строка подключения не содержит корректный IP-адрес.</exception>
    private IPAddress ResolveIpAddress()
    {
      if (IPAddress.TryParse(_device.ConnectionDetails, out IPAddress? ipAddress))
      {
        return ipAddress;
      }

      throw new InvalidOperationException($"[{_device.Name}] Не удалось получить корректный IP-адрес из ConnectionDetails.");
    }

    /// <summary>
    /// Получает последний октет IP-адреса.
    /// </summary>
    /// <param name="ipAddress">IP-адрес.</param>
    /// <returns>Целое значение последнего октета.</returns>
    /// <exception cref="ArgumentException">Выбрасывается, если IP-адрес имеет некорректный формат.</exception>
    private static int GetLastOctet(IPAddress ipAddress)
    {
      string[] parts = ipAddress.ToString().Split('.');
      if (parts.Length != 4 || !int.TryParse(parts[3], out int lastOctet))
      {
        throw new ArgumentException("Некорректный IP-адрес для определения порта.");
      }

      return lastOctet;
    }
  }
}
