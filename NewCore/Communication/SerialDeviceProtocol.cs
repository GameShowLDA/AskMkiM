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
    public async Task<string> QueryAsync(string command, int responseDelay = 0, int timeout = 0)
    {
      try
      {
        if (!EnsurePortOpen())
        {
          return string.Empty;
        }

        LogInformation($"[{_device.Name}] Отправка команды: \"{command}\" в порт {_serialPort.PortName}");

        _serialPort.WriteLine(command);

        if (responseDelay > 0)
        {
          await Task.Delay(responseDelay);
        }

        if (timeout > 0)
        {
          _serialPort.ReadTimeout = timeout;

          return await Task.Run(() =>
          {
            try
            {
              string response = _serialPort.ReadLine();
              LogInformation($"[{_device.Name}] Ответ от устройства: {response}");
              return response;
            }
            catch (TimeoutException)
            {
              LogWarning($"[{_device.Name}] Время ожидания ответа истекло ({timeout} мс)");
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
        if (_serialPort.IsOpen)
        {
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
        LogError($"[{_device.Name}] Доступ к порту запрещен: {ex.Message}");
        return false;
      }
      catch (IOException ex)
      {
        LogError($"[{_device.Name}] Ошибка ввода-вывода при открытии порта: {ex.Message}");
        return false;
      }
      catch (InvalidOperationException ex)
      {
        LogError($"[{_device.Name}] Порт уже используется другим процессом: {ex.Message}");
        return false;
      }
      catch (Exception ex)
      {
        LogError($"[{_device.Name}] Неизвестная ошибка при открытии порта: {ex.Message}");
        return false;
      }
    }
  }
}
