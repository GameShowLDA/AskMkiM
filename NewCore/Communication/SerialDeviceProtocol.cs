using System;
using System.IO.Ports;
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

    private static readonly SemaphoreSlim _portLock = new(1, 1);
    private bool _disposed;

    /// <summary>
    /// Инициализирует новый экземпляр <see cref="SerialDeviceProtocol"/>.
    /// </summary>
    /// <param name="device">Устройство с COM-подключением.</param>
    /// <param name="serialPort">Объект SerialPort, уже настроенный.</param>
    public SerialDeviceProtocol(DeviceWithCOM device, SerialPort serialPort)
    {
      _device = device ?? throw new ArgumentNullException(nameof(device));
      _serialPort = serialPort ?? throw new ArgumentNullException(nameof(serialPort));

      EnsurePortOpen();
    }

    /// <inheritdoc />
    public async Task<string> QueryAsync(string command, double responseDelay = 100, int timeout = 0, int port = 0, int delayBeforeCall = 0)
    {
      await _portLock.WaitAsync();
      try
      {
        if (delayBeforeCall > 0)
        {
          LogDebug($"Задержка перед вызовом: {delayBeforeCall} мс");
          await Task.Delay(delayBeforeCall);
        }

        LogInformation($"[{_device.Name}] Отправка команды: \"{command}\" в порт {_serialPort.PortName}");

        _serialPort.DiscardInBuffer();
        _serialPort.DiscardOutBuffer();
        _serialPort.WriteLine(command);

        if (timeout > 0)
        {
          _serialPort.ReadTimeout = timeout;
          int roundedDelay = (int)Math.Ceiling(responseDelay) + 100;
          LogDebug($"Задержка перед чтением ответа: {roundedDelay} мс");
          await Task.Delay(roundedDelay);

          return await Task.Run(() =>
          {
            try
            {
              LogDebug("Ожидание ответа от устройства...");
              string response = _serialPort.ReadLine();
              LogInformation($"[{_device.Name}] Ответ от устройства: {response}");
              return response;
            }
            catch (TimeoutException)
            {
              LogWarning($"[{_device.Name}] Время ожидания ответа истекло ({timeout} мс)");
              return string.Empty;
            }
            catch (Exception ex)
            {
              LogError($"[{_device.Name}] Ошибка при чтении из порта: {ex.Message}");
              return string.Empty;
            }
          });
        }

        return string.Empty;
      }
      catch (Exception ex)
      {
        LogError($"[{_device.Name}] Ошибка при работе с COM-портом: {ex.Message}");
        return string.Empty;
      }
      finally
      {
        _portLock.Release();
      }
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
          LogInformation($"[{_device.Name}] Порт {_serialPort.PortName} успешно открыт.");
        }
        return true;
      }
      catch (Exception ex)
      {
        LogException($"[{_device.Name}] Ошибка при открытии порта", ex);
        return false;
      }
    }

    public void Dispose()
    {
      if (!_disposed)
      {
        try
        {
          if (_serialPort.IsOpen)
          {
            _serialPort.Close();
            LogInformation($"[{_device.Name}] Порт {_serialPort.PortName} закрыт.");
          }
        }
        catch (Exception ex)
        {
          LogException($"[{_device.Name}] Ошибка при закрытии порта", ex);
        }
        _disposed = true;
      }
    }
  }
}
