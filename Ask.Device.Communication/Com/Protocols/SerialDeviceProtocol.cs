using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Device.Communication.Common;
using System.Diagnostics;
using System.IO.Ports;
using System.Text;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Device.Communication.Com
{
  /// <summary>
  /// Реализует транспортный протокол обмена с устройствами по COM-порту.
  /// </summary>
  public class SerialDeviceProtocol : IDeviceProtocol
  {
    /// <summary>
    /// Настроенный последовательный порт устройства.
    /// </summary>
    private readonly SerialPort _serialPort;

    /// <summary>
    /// Устройство, к которому относится текущий COM-протокол.
    /// </summary>
    private readonly DeviceWithCOM _device;

    /// <summary>
    /// Семафор, запрещающий параллельный доступ к одному COM-порту.
    /// </summary>
    public SemaphoreSlim OperationLock { get; set; }

    /// <summary>
    /// Инициализирует новый экземпляр <see cref="SerialDeviceProtocol"/>.
    /// </summary>
    /// <param name="device">Устройство с COM-подключением.</param>
    /// <param name="serialPort">Настроенный объект последовательного порта.</param>
    public SerialDeviceProtocol(DeviceWithCOM device, SerialPort serialPort)
    {
      _device = device ?? throw new ArgumentNullException(nameof(device));
      _serialPort = serialPort ?? throw new ArgumentNullException(nameof(serialPort));
      OperationLock = new SemaphoreSlim(1, 1);
    }

    /// <summary>
    /// Отправляет команду в COM-порт и при необходимости ожидает ответ от устройства.
    /// </summary>
    /// <param name="command">Текст команды для отправки.</param>
    /// <param name="responseDelay">Задержка перед чтением ответа в миллисекундах.</param>
    /// <param name="timeout">Таймаут ожидания ответа в миллисекундах.</param>
    /// <param name="port">Параметр интерфейса, не используемый COM-протоколом.</param>
    /// <param name="delayBeforeCall">Задержка перед отправкой команды в миллисекундах.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Строка ответа устройства или пустая строка при ошибке либо отсутствии ответа.</returns>
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
    /// Проверяет текущее состояние COM-порта и пишет его в лог.
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
    /// Асинхронно ожидает указанную задержку.
    /// </summary>
    /// <param name="delay">Время задержки в миллисекундах.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    private async Task WaitAsync(int delay, CancellationToken cancellationToken)
    {
      await Task.Delay(delay, cancellationToken);
    }

    /// <summary>
    /// Отправляет команду в COM-порт.
    /// </summary>
    /// <param name="command">Строка команды.</param>
    private void SendCommand(string command)
    {
      LogInformation($"[{_device.Name}] Отправка команды: \"{command}\" в порт {_serialPort.PortName}", isDeviceLog: true);
      _serialPort.DiscardInBuffer();
      _serialPort.DiscardOutBuffer();
      _serialPort.Write(command + "\n");
    }

    /// <summary>
    /// Асинхронно ожидает ответ от устройства в течение указанного таймаута.
    /// </summary>
    /// <param name="timeout">Максимальное время ожидания ответа в миллисекундах.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
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
          {
            break;
          }
        }

        await Task.Delay(20, cancellationToken);
      }

      string response = responseBuilder.ToString().Trim();

      if (string.IsNullOrWhiteSpace(response))
      {
        LogWarning($"[{_device.Name}] Таймаут: данных от устройства нет за {timeout} мс", isDeviceLog: true);
      }
      else
      {
        LogInformation($"[{_device.Name}] Ответ от устройства: {response}", isDeviceLog: true);
      }

      return response;
    }
  }
}
