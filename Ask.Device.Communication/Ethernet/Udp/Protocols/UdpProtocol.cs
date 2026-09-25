using Ask.Core.Services.Errors.Device;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Device.Communication.Common.Threading;
using Ask.Diagnostics.Services;
using System.Net;
using System.Net.Sockets;
using System.Text;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Device.Communication.Ethernet.Udp.Protocols
{
  /// <summary>
  /// Реализует универсальный транспортный протокол обмена с устройствами по UDP.
  /// </summary>
  public class UdpProtocol : IDeviceProtocol, IDisposable
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
    private readonly object _socketGate = new ();
    private readonly Func<int, UdpClient> _createClient;
    private readonly Func<UdpClient, ReadOnlyMemory<byte>, IPEndPoint, CancellationToken, ValueTask<int>> _send;
    private UdpClient? _client;
    private IPEndPoint? _endpoint;
    private int _inputPort;
    private bool _disposed;

    /// <summary>
    /// Получает или задаёт семафор, запрещающий параллельную отправку команд в одно устройство.
    /// </summary>
    public SemaphoreSlim OperationLock { get; set; }

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="UdpProtocol"/>.
    /// </summary>
    /// <param name="device">Устройство, использующее протокол.</param>
    public UdpProtocol(IDevice device)
      : this(device,
          port => new UdpClient(new IPEndPoint(IPAddress.Any, port)),
          static (client, buffer, endpoint, token) => client.SendAsync(buffer, endpoint, token))
    {
    }

    internal UdpProtocol(
      IDevice device,
      Func<int, UdpClient> createClient,
      Func<UdpClient, ReadOnlyMemory<byte>, IPEndPoint, CancellationToken, ValueTask<int>> send)
    {
      _device = device ?? throw new ArgumentNullException(nameof(device));
      _createClient = createClient;
      _send = send;
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
          var udpClient = GetClient(deviceEndpoint, inputPort);
          byte[] buffer = Encoding.UTF8.GetBytes(command);

          await DrainPendingResponsesAsync(udpClient, cancellationToken).ConfigureAwait(false);
          try
          {
            await _send(udpClient, buffer, deviceEndpoint, cancellationToken).ConfigureAwait(false);
          }
          catch (SocketException ex) when (ex.SocketErrorCode == SocketError.NoBufferSpaceAvailable)
          {
            // Повторяем только локально отклонённую отправку, а не запрос после тайм-аута ответа.
            CloseClient();
            LogWarning($"[{_device.Name}] UDP: недостаточно ресурсов отправки (10055). Повтор через 50 мс.", isDeviceLog: true);
            await Task.Delay(50, cancellationToken).ConfigureAwait(false);
            udpClient = GetClient(deviceEndpoint, inputPort);
            await _send(udpClient, buffer, deviceEndpoint, cancellationToken).ConfigureAwait(false);
          }
          DiagnosticCommandHistory.RecordCommand(_device.Name, command);
          LogInformation($"[{_device.Name}] Отправка команды: \"{command}\" на {deviceEndpoint}", isDeviceLog: true);

          if (responseDelay > 0)
          {
            await Task.Delay((int)Math.Ceiling(responseDelay), cancellationToken).ConfigureAwait(false);
          }

          if (timeout <= 0)
          {
            CloseClient();
            return string.Empty;
          }

          using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
          timeoutCts.CancelAfter(timeout);

          UdpReceiveResult result;
          do
          {
            result = await udpClient.ReceiveAsync(timeoutCts.Token).ConfigureAwait(false);
            // Прошивка может отвечать с другого порта; проверяем IP, сохраняя совместимость.
          }
          while (!result.RemoteEndPoint.Address.Equals(ipAddress));
          string response = Encoding.UTF8.GetString(result.Buffer);
          DiagnosticCommandHistory.RecordResponse(_device.Name, response);
          LogInformation($"[{_device.Name}] Ответ от устройства: {response}", isDeviceLog: true);
          return response;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
          CloseClient();
          return LogWarning($"[{_device.Name}] Устройство не ответило в течение {timeout / 1000.0} секунд(ы).", isDeviceLog: true);
        }
        catch (OperationCanceledException)
        {
          CloseClient();
          throw;
        }
        catch (Exception ex)
        {
          CloseClient();
          LogException($"[{_device.Name}] Ошибка UDP QueryAsync", ex, isDeviceLog: true);
          throw new DeviceTransportException($"[{_device.Name}] Ошибка UDP-транспорта: {ex.Message}", ex);
        }
      }
    }

    private UdpClient GetClient(IPEndPoint endpoint, int inputPort)
    {
      lock (_socketGate)
      {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_client != null && endpoint.Equals(_endpoint) && inputPort == _inputPort)
          return _client;

        CloseClient();
        _client = _createClient(inputPort);
        _endpoint = endpoint;
        _inputPort = inputPort;
        return _client;
      }
    }

    private async Task DrainPendingResponsesAsync(UdpClient client, CancellationToken token)
    {
      // Poll обнаруживает и пустые датаграммы. Ожидания на успешном пути нет.
      while (client.Client.Poll(0, SelectMode.SelectRead))
      {
        token.ThrowIfCancellationRequested();
        await client.ReceiveAsync(token).ConfigureAwait(false);
      }
    }

    private void CloseClient()
    {
      lock (_socketGate)
      {
        _client?.Dispose();
        _client = null;
        _endpoint = null;
      }
    }

    /// <summary>
    /// Закрывает UDP-сокет, в том числе при незавершённом обмене.
    /// </summary>
    public void Dispose()
    {
      lock (_socketGate)
      {
        _disposed = true;
        CloseClient();
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
