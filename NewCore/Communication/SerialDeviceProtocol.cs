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

    /// <summary>
    /// Инициализирует новый экземпляр <see cref="SerialDeviceProtocol"/>.
    /// </summary>
    /// <param name="device">Устройство с COM-подключением.</param>
    /// <param name="serialPort">Объект SerialPort, уже настроенный.</param>
    public SerialDeviceProtocol(DeviceWithCOM device, SerialPort serialPort)
    {
      _device = device ?? throw new ArgumentNullException(nameof(device));
      _serialPort = serialPort ?? throw new ArgumentNullException(nameof(serialPort));
    }

    /// <inheritdoc />
    public async Task<string> QueryAsync(string command, double responseDelay = 0, int timeout = 0, int port = 0, int delayBeforeCall = 0)
    {
      if (delayBeforeCall > 0)
      {
        LogDebug($"Задержка перед вызовом: {delayBeforeCall} мс");
        await Task.Delay(delayBeforeCall);
      }

      try
      {
        if (!EnsurePortOpen())
        {
          LogWarning($"COM-порт не удалось открыть: {_serialPort.PortName}");
          return string.Empty;
        }

        LogDebug($"COM-порт открыт: {_serialPort.IsOpen}, Скорость: {_serialPort.BaudRate}, Handshake: {_serialPort.Handshake}");
        LogInformation($"[{_device.Name}] Отправка команды: \"{command}\" в порт {_serialPort.PortName}");

        _serialPort.DiscardInBuffer();  // очистка входного буфера перед отправкой
        _serialPort.DiscardOutBuffer(); // очистка выходного буфера

        _serialPort.WriteLine(command);

        LogDebug($"Команда отправлена. BytesToRead до задержки: {_serialPort.BytesToRead}");

        if (responseDelay > 0)
        {
          int roundedDelay = (int)Math.Ceiling(responseDelay) + 300;
          LogDebug($"Задержка перед чтением ответа: {roundedDelay} мс");
          await Task.Delay(roundedDelay);
        }

        if (timeout > 0)
        {
          _serialPort.ReadTimeout = timeout;

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

        LogDebug("Таймаут чтения не задан, возвращается пустой ответ.");
        return string.Empty;
      }
      catch (Exception ex)
      {
        LogError($"[{_device.Name}] Ошибка при работе с COM-портом: {ex.Message}");
        return string.Empty;
      }
      finally
      {
        if (_serialPort.IsOpen)
        {
          LogDebug("Закрытие COM-порта.");
          _serialPort.Close();
        }
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
      catch (UnauthorizedAccessException ex)
      {
        LogException($"[{_device.Name}] Доступ к порту запрещен", ex);
        return false;
      }
      catch (IOException ex)
      {
        LogException($"[{_device.Name}] Ошибка ввода-вывода при открытии порта", ex);
        return false;
      }
      catch (InvalidOperationException ex)
      {
        LogException($"[{_device.Name}] Порт уже используется другим процессом", ex);
        return false;
      }
      catch (Exception ex)
      {
        LogException($"[{_device.Name}] Неизвестная ошибка при открытии порта", ex);
        return false;
      }
    }
  }
}
