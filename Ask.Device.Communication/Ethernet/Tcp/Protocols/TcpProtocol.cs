using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Device.Communication.Common.Threading;
using System.IO;
using System.Net.Sockets;
using System.Text;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Device.Communication.Ethernet.Tcp.Protocols
{
  /// <summary>
  /// Реализует универсальный транспортный протокол обмена с устройствами по TCP.
  /// </summary>
  public class TcpProtocol : IDeviceProtocol
  {
    /// <summary>
    /// Устройство, для которого выполняется обмен.
    /// </summary>
    private readonly IDevice _device;

    /// <summary>
    /// Порт подключения по умолчанию.
    /// </summary>
    private readonly int _defaultPort;

    /// <summary>
    /// Текущий TCP-клиент подключения.
    /// </summary>
    private TcpClient? _client;

    /// <summary>
    /// Текущий сетевой поток TCP-подключения.
    /// </summary>
    private NetworkStream? _stream;

    /// <summary>
    /// Хост, к которому сейчас установлено соединение.
    /// </summary>
    private string _connectedHost = string.Empty;

    /// <summary>
    /// Порт, к которому сейчас установлено соединение.
    /// </summary>
    private int _connectedPort;

    /// <summary>
    /// Получает или задаёт семафор, запрещающий параллельный доступ к одному TCP-соединению.
    /// </summary>
    public SemaphoreSlim OperationLock { get; set; }

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="TcpProtocol"/>.
    /// </summary>
    /// <param name="device">Устройство, использующее протокол.</param>
    /// <param name="defaultPort">Порт подключения по умолчанию.</param>
    public TcpProtocol(IDevice device, int defaultPort)
    {
      _device = device ?? throw new ArgumentNullException(nameof(device));
      _defaultPort = defaultPort;
      OperationLock = new SemaphoreSlim(1, 1);
    }

    /// <summary>
    /// Отправляет TCP-команду устройству и при необходимости ожидает ответ.
    /// </summary>
    /// <param name="command">Команда для отправки.</param>
    /// <param name="responseDelay">Задержка перед чтением ответа в миллисекундах.</param>
    /// <param name="timeout">Таймаут ожидания ответа в миллисекундах.</param>
    /// <param name="port">Пользовательский порт подключения. Если равен нулю, используется порт по умолчанию.</param>
    /// <param name="delayBeforeCall">Задержка перед отправкой команды в миллисекундах.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Ответ устройства или пустая строка, если ответ не был получен.</returns>
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

          string host = ResolveHost();
          int effectivePort = port > 0 ? port : _defaultPort;

          await EnsureConnectionAsync(host, effectivePort, cancellationToken).ConfigureAwait(false);
          await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);

          if (responseDelay > 0)
          {
            await Task.Delay((int)Math.Ceiling(responseDelay), cancellationToken).ConfigureAwait(false);
          }

          if (timeout <= 0)
          {
            return string.Empty;
          }

          if (_stream == null || !_stream.CanRead)
          {
            throw new InvalidOperationException("TCP-поток недоступен для чтения.");
          }

          byte[] buffer = new byte[1024];
          using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
          timeoutCts.CancelAfter(timeout);

          int bytesRead = await _stream.ReadAsync(buffer, timeoutCts.Token).ConfigureAwait(false);
          string answer = Encoding.ASCII.GetString(buffer, 0, bytesRead).Trim();
          LogInformation($"[{_device.Name}] Получен ответ ({command}) - {answer}", isDeviceLog: true);
          return answer;
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
          CloseConnection();
          LogException(new TimeoutException("Read operation timed out.", ex), isDeviceLog: true);
          return string.Empty;
        }
        catch (IOException ioEx)
        {
          CloseConnection();
          LogException(ioEx, isDeviceLog: true);
          return string.Empty;
        }
        catch (SocketException socketEx)
        {
          CloseConnection();
          LogException(socketEx, isDeviceLog: true);
          return string.Empty;
        }
        catch (Exception ex)
        {
          LogException(ex, isDeviceLog: true);
          return string.Empty;
        }
      }
    }

    /// <summary>
    /// Возвращает актуальный хост подключения устройства.
    /// </summary>
    /// <returns>Строка хоста подключения.</returns>
    /// <exception cref="InvalidOperationException">Выбрасывается, если хост подключения не задан.</exception>
    private string ResolveHost()
    {
      if (string.IsNullOrWhiteSpace(_device.ConnectionDetails))
      {
        throw new InvalidOperationException($"[{_device.Name}] Не задан адрес TCP-подключения.");
      }

      return _device.ConnectionDetails;
    }

    /// <summary>
    /// Гарантирует наличие открытого TCP-подключения к актуальному хосту и порту.
    /// </summary>
    /// <param name="host">Хост подключения.</param>
    /// <param name="port">Порт подключения.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    private async Task EnsureConnectionAsync(string host, int port, CancellationToken cancellationToken)
    {
      bool connectionMatches = _client is { Connected: true } &&
                               _stream is { CanRead: true, CanWrite: true } &&
                               string.Equals(_connectedHost, host, StringComparison.OrdinalIgnoreCase) &&
                               _connectedPort == port;

      if (connectionMatches)
      {
        return;
      }

      CloseConnection();

      _client = new TcpClient();
      await _client.ConnectAsync(host, port, cancellationToken).ConfigureAwait(false);
      _stream = _client.GetStream();
      _connectedHost = host;
      _connectedPort = port;
    }

    /// <summary>
    /// Отправляет TCP-команду без ожидания ответа.
    /// </summary>
    /// <param name="command">Команда для отправки.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    private async Task SendCommandAsync(string command, CancellationToken cancellationToken)
    {
      if (_stream == null)
      {
        throw new InvalidOperationException("TCP-поток не инициализирован.");
      }

      byte[] data = Encoding.ASCII.GetBytes(command + "\n");
      await _stream.WriteAsync(data, cancellationToken).ConfigureAwait(false);
      LogInformation($"[{_device.Name}] Отправка команды - {command}", isDeviceLog: true);
    }

    /// <summary>
    /// Закрывает текущее TCP-подключение и очищает состояние протокола.
    /// </summary>
    private void CloseConnection()
    {
      try
      {
        _stream?.Dispose();
      }
      catch
      {
      }

      try
      {
        _client?.Dispose();
      }
      catch
      {
      }

      _stream = null;
      _client = null;
      _connectedHost = string.Empty;
      _connectedPort = 0;
    }
  }
}
