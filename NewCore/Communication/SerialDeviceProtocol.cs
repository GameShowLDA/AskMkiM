using System;
using System.Diagnostics;
using System.IO.Ports;
using System.Text;
using System.Threading.Tasks;
using NewCore.Base.Device;
using static Utilities.LoggerUtility;

namespace NewCore.Communication
{
  /// <summary>
  /// Реализация <see cref="IDeviceProtocol"/> для работы с устройствами по COM-порту.
  /// </summary>
  public class SerialDeviceProtocol : IDeviceProtocol
  {
    private readonly SerialPort _serialPort;
    private readonly DeviceWithCOM _device;

    private static readonly SemaphoreSlim _portLock = new SemaphoreSlim(1, 1);

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
      SerialPortHelper.RegisterSerialPort(_serialPort);
      OperationLock = new SemaphoreSlim(1, 1);
    }

    /// <inheritdoc />
    public async Task<string> QueryAsync(string command, double responseDelay = 0, int timeout = 0, int port = 0, int delayBeforeCall = 0)
    {
      LogDebug($"[{_device.Name}] Захват _portLock", isDeviceLog: true);
      await OperationLock.WaitAsync();

      try
      {
        if (delayBeforeCall > 0)
          await DelayBeforeCall(delayBeforeCall);

        try
        {
          if (!EnsurePortOpen())
            return HandlePortOpenFailure();

          PreparePortForCommand(command);

          if (responseDelay > 0)
            await DelayBeforeRead(responseDelay);

          if (timeout > 0)
            return await ReadResponseWithTimeout(timeout);

          LogDebug("Таймаут чтения не задан, возвращается пустой ответ.", isDeviceLog: true);
          return string.Empty;
        }
        catch (Exception ex)
        {
          LogException(ex, $"[{_device.Name}] Ошибка при работе с COM-портом", isDeviceLog: true);
          return string.Empty;
        }
      }
      finally
      {
        LogDebug($"[{_device.Name}] Освобождение _portLock", isDeviceLog: true);
        OperationLock.Release();
      }
    }

    /// <summary>
    /// Выполняет задержку перед отправкой команды устройству.
    /// </summary>
    /// <param name="delayBeforeCall">Время задержки в миллисекундах.</param>
    private async Task DelayBeforeCall(int delayBeforeCall)
    {
      LogDebug($"Задержка перед вызовом: {delayBeforeCall} мс", isDeviceLog: true);
      await Task.Delay(delayBeforeCall);
    }

    /// <summary>
    /// Обрабатывает ситуацию, когда COM-порт не удалось открыть.
    /// Логирует предупреждение и возвращает пустую строку.
    /// </summary>
    private string HandlePortOpenFailure()
    {
      LogWarning($"COM-порт не удалось открыть: {_serialPort.PortName}", isDeviceLog: true);
      return string.Empty;
    }

    /// <summary>
    /// Подготавливает COM-порт к отправке команды: очищает буферы и отправляет команду.
    /// </summary>
    /// <param name="command">Команда для отправки устройству.</param>
    private void PreparePortForCommand(string command)
    {
      LogDebug($"COM-порт открыт: {_serialPort.IsOpen}, Скорость: {_serialPort.BaudRate}, Handshake: {_serialPort.Handshake}", isDeviceLog: true);
      LogInformation($"[{_device.Name}] Отправка команды: \"{command}\" в порт {_serialPort.PortName}", isDeviceLog: true);

      _serialPort.DiscardInBuffer();
      _serialPort.DiscardOutBuffer();
      _serialPort.WriteLine(command);

      LogDebug($"Команда отправлена. BytesToRead до задержки: {_serialPort.BytesToRead}", isDeviceLog: true);
    }

    private async Task DelayBeforeRead(double responseDelay)
    {
      int roundedDelay = (int)Math.Ceiling(responseDelay) + 300;
      LogDebug($"Задержка перед чтением ответа: {roundedDelay} мс", isDeviceLog: true);
      await Task.Delay(roundedDelay);
    }

    /// <summary>
    /// Выполняет задержку перед чтением ответа от устройства.
    /// </summary>
    /// <param name="responseDelay">Время задержки в миллисекундах.</param>
    private async Task<string> ReadResponseWithTimeout(int timeout)
    {
      _serialPort.ReadTimeout = timeout;
      var responseBuilder = new StringBuilder();
      var stopwatch = Stopwatch.StartNew();

      while (stopwatch.ElapsedMilliseconds < timeout)
      {
        try
        {
          int availableBytes = _serialPort.BytesToRead;
          if (availableBytes > 0)
          {
            string chunk = _serialPort.ReadExisting();
            responseBuilder.Append(chunk);
            LogDebug($"Принятый фрагмент: {chunk.Trim()}", isDeviceLog: true);
            if (chunk.Contains("\n") || chunk.Contains("\r")) break;
          }
          else
          {
            await Task.Delay(10); 
          }
        }
        catch (Exception ex)
        {
          LogException(ex, $"Ошибка при чтении из порта", isDeviceLog: true);
          break;
        }
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

    /// <summary>
    /// Безопасно открывает COM-порт, если он ещё не открыт.
    /// </summary>
    /// <returns>True, если порт успешно открыт или уже был открыт.</returns>
    private bool EnsurePortOpen()
    {
      try
      {
        if (!_serialPort.IsOpen)
        {
          _serialPort.Open();
          LogInformation($"[{_device.Name}] Порт {_serialPort.PortName} успешно открыт.", isDeviceLog: true);
        }

        return true;
      }
      catch (UnauthorizedAccessException ex)
      {
        LogException($"[{_device.Name}] Доступ к порту запрещен", ex, isDeviceLog: true);
        return false;
      }
      catch (IOException ex)
      {
        LogException($"[{_device.Name}] Ошибка ввода-вывода при открытии порта", ex, isDeviceLog: true);
        return false;
      }
      catch (InvalidOperationException ex)
      {
        LogException($"[{_device.Name}] Порт уже используется другим процессом", ex, isDeviceLog: true);
        return false;
      }
      catch (Exception ex)
      {
        LogException($"[{_device.Name}] Неизвестная ошибка при открытии порта", ex, isDeviceLog: true);
        return false;
      }
    }
  }
}