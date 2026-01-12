using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using NewCore.Base.Device;
using System.Diagnostics;
using System.IO.Ports;
using System.Text;
using static Ask.LogLib.LoggerUtility;

namespace NewCore.Communication
{
  /// <summary>
  /// Реализация <see cref="IDeviceProtocol"/> для работы с устройствами по COM-порту.
  /// </summary>
  public class SerialDeviceProtocol : IDeviceProtocol
  {
    private readonly SerialPort _serialPort;
    private readonly DeviceWithCOM _device;

    public SemaphoreSlim OperationLock { get; set; }


    /// <summary>
    /// Инициализирует новый экземпляр <see cref="SerialDeviceProtocol"/>.
    /// </summary>
    /// <param name="device">Устройство с COM-подключением.</param>
    /// <param name="serialPort">Объект SerialPort, уже настроенный.</param>
    public SerialDeviceProtocol(DeviceWithCOM device, SerialPort serialPort)
    {
      _device = device ?? throw new ArgumentNullException(nameof(device));
      _serialPort = serialPort ?? throw new ArgumentNullException(nameof(serialPort));
      OperationLock = new SemaphoreSlim(1, 1);
    }

    /// <inheritdoc />
    public async Task<string> QueryAsync(string command, double responseDelay = 0, int timeout = 0, int port = 0, int delayBeforeCall = 0, CancellationToken cancellationToken = new CancellationToken())
    {
      using (await OperationLock.LockAsync(cancellationToken))
      {
        using (await _serialPort.UsePort())
        {
          try
          {
            CheckComPort();

            await WaitAsync(delayBeforeCall, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            SendCommand(command);

            await WaitAsync((int)Math.Ceiling(responseDelay), cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            var answer = await ReadResponseAsync(timeout, cancellationToken);
            return answer;
          }
          catch (Exception ex)
          {
            LogException(ex, $"[{_device.Name}] Ошибка при работе с COM-портом", isDeviceLog: true);
            return string.Empty;
          }
        }
      }
    }

    /// <summary>
    /// Логирует текущее состояние COM-порта.
    /// </summary>
    private void CheckComPort()
    {
      if (_serialPort == null)
      {
        LogError($"[{_device.Name}] COM-порт не инициализирован.", isDeviceLog: true);
        return;
      }

      if (_serialPort.IsOpen)
      {
        LogInformation($"[{_device.Name}] COM-порт {_serialPort.PortName} открыт и доступен.", isDeviceLog: true);
      }
      else
      {
        LogWarning($"[{_device.Name}] COM-порт {_serialPort.PortName} закрыт, будет открыт автоматически.", isDeviceLog: true);
      }
    }

    /// <summary>
    /// Асинхронное ожидание указанной задержки.
    /// </summary>
    /// <param name="milliseconds">Время задержки в миллисекундах.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    private async Task WaitAsync(int delay, CancellationToken cancellationToken)
    {
      await Task.Delay(delay, cancellationToken);
    }

    /// <summary>
    /// Отправка команды.
    /// </summary>
    /// <param name="cmd">Строка команды.</param>
    private void SendCommand(string cmd)
    {
      LogInformation($"[{_device.Name}] Отправка команды: \"{cmd}\" в порт {_serialPort.PortName}", isDeviceLog: true);
      _serialPort.DiscardInBuffer();
      _serialPort.DiscardOutBuffer();
      _serialPort.Write(cmd + "\n");
    }

    /// <summary>
    /// Асинхронно ожидает ответ от устройства в течение указанного таймаута.
    /// Проверка входящего буфера выполняется каждые 100 мс.
    /// Как только получен ответ — метод немедленно возвращает его.
    /// </summary>
    /// <param name="timeout">Максимальное время ожидания (мс).</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Ответ устройства или пустая строка при таймауте.</returns>
    private async Task<string> ReadResponseAsync(int timeout, CancellationToken cancellationToken)
    {
      var responseBuilder = new StringBuilder();
      var stopwatch = Stopwatch.StartNew();

      while (stopwatch.ElapsedMilliseconds < timeout)
      {
        cancellationToken.ThrowIfCancellationRequested();

        if (_serialPort.BytesToRead > 0)
        {
          string chunk = _serialPort.ReadExisting();
          responseBuilder.Append(chunk);

          if (responseBuilder.ToString().Contains("\n"))
            break;
        }

        await Task.Delay(20, cancellationToken);
      }

      string response = responseBuilder.ToString().Trim();

      if (string.IsNullOrWhiteSpace(response))
        LogWarning($"[{_device.Name}] Таймаут: данных от устройства нет за {timeout} мс", isDeviceLog: true);
      else
        LogInformation($"[{_device.Name}] Ответ от устройства: {response}", isDeviceLog: true);

      return response;
    }


  }
}