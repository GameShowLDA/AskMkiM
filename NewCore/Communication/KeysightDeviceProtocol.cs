using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using System.IO;
using System.Net.Sockets;
using System.Text;
using static Ask.LogLib.LoggerUtility;

namespace NewCore.Communication
{
  /// <summary>
  /// Реализация <see cref="IDeviceProtocol"/> для общения с мультиметрами Keysight.
  /// </summary>
  public class KeysightDeviceProtocol : IDeviceProtocol
  {
    private readonly IFastMeter _device;
    private readonly int _port;

    /// <summary>
    /// Сетевой поток для передачи команд и получения данных.
    /// </summary>
    static internal NetworkStream Stream { get; set; }

    /// <summary>
    /// TCP-клиент для установления соединения с устройством.
    /// </summary>
    static internal TcpClient Client { get; set; }
    public SemaphoreSlim OperationLock { get; set; }

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="KeysightDeviceProtocol"/>.
    /// </summary>
    /// <param name="device">Устройство измерения, с которым устанавливается связь.</param>
    /// <param name="port">Порт, на котором выполняется подключение к устройству.</param>
    public KeysightDeviceProtocol(IFastMeter device, int port)
    {
      _device = device ?? throw new ArgumentNullException(nameof(device));
      _port = port;
      OperationLock = new SemaphoreSlim(1, 1);
    }

    /// <inheritdoc />
    public async Task<string> QueryAsync(string command, double responseDelay = 0, int timeout = 0, int port = 0, int delayBeforeCall = 0, CancellationToken cancellationToken = new CancellationToken())
    {
      try
      {
        if (Client == null || !Client.Connected)
        {
          await EstablishConnection();
        }

        await SendCommandAsync(command);

        if (responseDelay > 0)
        {
          int roundedDelay = (int)Math.Ceiling(responseDelay);
          await Task.Delay(roundedDelay);
        }

        if (timeout > 0)
        {
          if (Stream == null || !Stream.CanRead)
          {
            throw new InvalidOperationException("Stream is not available for reading.");
          }

          byte[] buffer = new byte[1024];

          using var cts = new CancellationTokenSource(timeout);
          try
          {
            int bytesRead = await Stream.ReadAsync(buffer, 0, buffer.Length, cts.Token);
            return Encoding.ASCII.GetString(buffer, 0, bytesRead).Trim();
          }
          catch (OperationCanceledException)
          {
            LogException(new TimeoutException("Read operation timed out."), isDeviceLog: true);
          }
          catch (IOException ioEx)
          {
            LogException(ioEx, isDeviceLog: true); // Проблема с потоком/подключением
          }
          catch (Exception innerEx)
          {
            LogException(innerEx, isDeviceLog: true);
          }
        }
      }
      catch (Exception ex)
      {
        LogException(ex, isDeviceLog: true); // Глобальный отлов
      }

      return string.Empty;
    }


    /// <summary>
    /// Устанавливает TCP-соединение с устройством, если оно ещё не подключено.
    /// </summary>
    private async Task EstablishConnection()
    {
      Client = new TcpClient();
      await Client.ConnectAsync(host: _device.ConnectionDetails, _port);
      Stream = Client.GetStream();
    }

    /// <summary>
    /// Отправляет SCPI-команду без ожидания ответа.
    /// </summary>
    /// <param name="command">SCPI-команда для отправки.</param>
    /// <returns>Задача, представляющая асинхронную операцию.</returns>
    private async Task SendCommandAsync(string command)
    {
      byte[] data = Encoding.ASCII.GetBytes(command + "\n");
      await Stream.WriteAsync(data, 0, data.Length);
    }
  }
}
