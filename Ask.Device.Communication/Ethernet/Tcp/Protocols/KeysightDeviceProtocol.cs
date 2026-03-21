using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Device.Communication.Common;
using System.IO;
using System.Net.Sockets;
using System.Text;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Device.Communication.Ethernet.Tcp
{
  /// <summary>
  /// Реализует протокол обмена с мультиметрами Keysight по TCP.
  /// </summary>
  public class KeysightDeviceProtocol : IDeviceProtocol
  {
    /// <summary>
    /// Устройство измерения, для которого выполняется обмен.
    /// </summary>
    private readonly IFastMeter _device;

    /// <summary>
    /// TCP-порт устройства.
    /// </summary>
    private readonly int _port;

    /// <summary>
    /// Сетевой поток для передачи команд и получения ответов.
    /// </summary>
    internal static NetworkStream? Stream { get; set; }

    /// <summary>
    /// TCP-клиент, удерживающий текущее соединение с устройством.
    /// </summary>
    internal static TcpClient? Client { get; set; }

    /// <summary>
    /// Семафор, запрещающий параллельный доступ к одному сокету.
    /// </summary>
    public SemaphoreSlim OperationLock { get; set; }

    /// <summary>
    /// Инициализирует новый экземпляр <see cref="KeysightDeviceProtocol"/>.
    /// </summary>
    /// <param name="device">Устройство измерения, с которым устанавливается связь.</param>
    /// <param name="port">Порт, на котором выполняется подключение.</param>
    public KeysightDeviceProtocol(IFastMeter device, int port)
    {
      _device = device ?? throw new ArgumentNullException(nameof(device));
      _port = port;
      OperationLock = new SemaphoreSlim(1, 1);
    }

    /// <summary>
    /// Отправляет TCP-команду в устройство Keysight и при необходимости ожидает ответ.
    /// </summary>
    /// <param name="command">Команда для отправки.</param>
    /// <param name="responseDelay">Задержка перед чтением ответа в миллисекундах.</param>
    /// <param name="timeout">Таймаут ожидания ответа в миллисекундах.</param>
    /// <param name="port">Параметр интерфейса, не используемый этим протоколом.</param>
    /// <param name="delayBeforeCall">Задержка перед отправкой команды в миллисекундах.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Ответ устройства или пустая строка, если ответ не получен.</returns>
    public async Task<string> QueryAsync(string command, double responseDelay = 0, int timeout = 0, int port = 0, int delayBeforeCall = 0, CancellationToken cancellationToken = new CancellationToken())
    {
      using (await OperationLock.LockAsync(cancellationToken))
      {
        try
        {
          if (delayBeforeCall > 0)
          {
            await Task.Delay(delayBeforeCall, cancellationToken).ConfigureAwait(false);
          }

          if (Client == null || !Client.Connected)
          {
            await EstablishConnectionAsync(cancellationToken).ConfigureAwait(false);
          }

          await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);

          if (responseDelay > 0)
          {
            await Task.Delay((int)Math.Ceiling(responseDelay), cancellationToken).ConfigureAwait(false);
          }

          if (timeout <= 0)
          {
            return string.Empty;
          }

          if (Stream == null || !Stream.CanRead)
          {
            throw new InvalidOperationException("Поток TCP недоступен для чтения.");
          }

          byte[] buffer = new byte[1024];
          using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
          timeoutCts.CancelAfter(timeout);

          int bytesRead = await Stream.ReadAsync(buffer, timeoutCts.Token).ConfigureAwait(false);
          var answer = Encoding.ASCII.GetString(buffer, 0, bytesRead).Trim();
          LogInformation($"{_device.Name} Получен ответ ({command}) - {answer}", isDeviceLog: true);
          return answer;
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
          LogException(new TimeoutException("Read operation timed out.", ex), isDeviceLog: true);
          return string.Empty;
        }
        catch (IOException ioEx)
        {
          LogException(ioEx, isDeviceLog: true);
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
    /// Устанавливает TCP-соединение с устройством, если оно ещё не создано.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены операции подключения.</param>
    private async Task EstablishConnectionAsync(CancellationToken cancellationToken)
    {
      Client = new TcpClient();
      await Client.ConnectAsync(_device.ConnectionDetails, _port, cancellationToken).ConfigureAwait(false);
      Stream = Client.GetStream();
    }

    /// <summary>
    /// Отправляет SCPI-команду без ожидания ответа.
    /// </summary>
    /// <param name="command">SCPI-команда для отправки.</param>
    /// <param name="cancellationToken">Токен отмены операции отправки.</param>
    private async Task SendCommandAsync(string command, CancellationToken cancellationToken)
    {
      if (Stream == null)
      {
        throw new InvalidOperationException("TCP-поток не инициализирован.");
      }

      byte[] data = Encoding.ASCII.GetBytes(command + "\n");
      await Stream.WriteAsync(data, cancellationToken).ConfigureAwait(false);
      LogInformation($"{_device.Name} Отправка команды - {command}", isDeviceLog: true);
    }
  }
}
