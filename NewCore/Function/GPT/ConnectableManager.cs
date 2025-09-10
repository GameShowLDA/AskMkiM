using NewCore.Base.Device;
using NewCore.Communication;
using NewCore.Device;
using NewCore.Function.GPT.Data;
using System.IO;
using System.IO.Ports;
using System.Management;
using System.Security.Cryptography;
using System.Xml.Linq;
using Utilities.Interface;
using static AppConfiguration.Execution.ExecutionConfig;
using static Utilities.LoggerUtility;

namespace NewCore.Function.GPT
{
  /// <summary>
  /// Класс для управления состоянием пробойной установки.
  /// </summary>
  public class ConnectableManager : IConnectable
  {

    /// <summary>
    /// Создает новый экземпляр класса <see cref="AcwMode"/>.
    /// </summary>
    /// <param name="gpt79904">Объект устройства GPT-79904.</param>
    public ConnectableManager(GPT79904 gpt79904) => _gptModel = gpt79904;

    /// <summary>
    /// Модель устройства GPT-79904.
    /// </summary>
    private GPT79904 _gptModel { get; set; }

    /// <inheritdoc />
    public async Task<(bool Connect, string Answer)> ConnectAsync(IUserMessageService _ = null)
    {
      if (_gptModel?.DeviceProtocol?.OperationLock == null)
        return (false, "DeviceProtocol не инициализирован");

      await _gptModel.DeviceProtocol.OperationLock.WaitAsync();
      try
      {
        if (_gptModel.COMPort == null)
          return (false, "COM-порт не инициализирован.");

        if (!_gptModel.COMPort.IsOpen)
        {
          _gptModel.COMPort.DtrEnable = true;
          _gptModel.COMPort.RtsEnable = true;
          _gptModel.COMPort.NewLine = "\r\n";
          _gptModel.COMPort.ReadTimeout = 2000;
          _gptModel.COMPort.WriteTimeout = 2000;

          _gptModel.COMPort.Open();
          LogInformation($"[{_gptModel.Name}] Порт {_gptModel.COMPort.PortName} открыт.", isDeviceLog: true);
          await InitializeAsync();
        }
        return (true, "");
      }
      catch (Exception ex)
      {
        if (await _gptModel.SystemManger.TestReset())
        {
          return (true, "");
        }

        LogException($"Ошибка открытия порта {_gptModel.COMPort?.PortName}", ex, isDeviceLog: true);
        return (false, ex.Message);
      }
      finally
      {
        _gptModel.DeviceProtocol.OperationLock.Release();

      }
    }

    /// <inheritdoc />
    public async Task<bool> DisconnectAsync(IUserMessageService _ = null)
    {
      if (_gptModel?.DeviceProtocol?.OperationLock == null)
        return true;

      await _gptModel.DeviceProtocol.OperationLock.WaitAsync();
      try
      {
        if (_gptModel?.COMPort?.IsOpen == true)
        {
          LogInformation($"[{_gptModel.Name}] Закрываю {_gptModel.COMPort.PortName}", isDeviceLog: true);
          await _gptModel.DeviceProtocol.QueryAsync("*RST");
          await _gptModel.DeviceProtocol.QueryAsync("*CLS");
          _gptModel.COMPort.Close();
        }
        return true;
      }
      catch (Exception ex)
      {
        LogException($"Ошибка закрытия порта {_gptModel?.COMPort?.PortName}", ex, isDeviceLog: true);
        return false;
      }
      finally
      {
        _gptModel.DeviceProtocol.OperationLock.Release();
      }
    }


    /// <inheritdoc />
    /// <summary>
    /// Выполняет инициализацию устройства GPT79904.
    /// Проверяет COM-порт, настраивает параметры и подготавливает протокол обмена.
    /// </summary>
    /// <param name="messageService">Сервис для отображения сообщений пользователю (опционально).</param>
    /// <returns>
    /// Кортеж: (bool Connect, string Answer)
    /// <para>Connect = true → инициализация успешна</para>
    /// <para>Connect = false → произошла ошибка, Answer содержит описание</para>
    /// </returns>
    public async Task<(bool Connect, string Answer)> InitializeAsync(IUserMessageService messageService = null)
    {

      try
      {
        // Если приложение в "холостом" режиме → ничего не делаем
        if (await GetIsIdleModeEnabled())
        {
          return (true, "Включен холостой режим");
        }

        // Проверяем наличие COM-порта
        if (_gptModel?.COMPort == null)
        {
          return (false, "COM-порт не инициализирован");
        }

        // Если протокол ещё не создан → создаём
        if (_gptModel.DeviceProtocol == null)
        {
          _gptModel.DeviceProtocol = new SerialDeviceProtocol(_gptModel, _gptModel.COMPort);
        }

        // Настраиваем параметры COM-порта (если не были заданы)
        if (_gptModel.COMPort.BaudRate <= 0) _gptModel.COMPort.BaudRate = 115200;
        if (_gptModel.COMPort.DataBits <= 0) _gptModel.COMPort.DataBits = 8;
        if (_gptModel.COMPort.Parity == Parity.None) _gptModel.COMPort.Parity = Parity.None;
        if (_gptModel.COMPort.StopBits == StopBits.None) _gptModel.COMPort.StopBits = StopBits.One;

        _gptModel.COMPort.DtrEnable = true;
        _gptModel.COMPort.RtsEnable = true;
        _gptModel.COMPort.NewLine = "\r\n";
        _gptModel.COMPort.ReadTimeout = 2000;
        _gptModel.COMPort.WriteTimeout = 2000;

        // Пробуем открыть порт, если он ещё не открыт
        if (!_gptModel.COMPort.IsOpen)
        {
          try
          {
            _gptModel.COMPort.Open();
            LogInformation($"[{_gptModel.Name}] Порт {_gptModel.COMPort.PortName} успешно открыт в InitializeAsync()", isDeviceLog: true);
          }
          catch (Exception)
          {
            await _gptModel.ConnectableManager.DisconnectAsync();
            _gptModel.COMPort.Open();
            LogInformation($"[{_gptModel.Name}] Порт {_gptModel.COMPort.PortName} успешно открыт в InitializeAsync()", isDeviceLog: true);
          }
        }
        else
        {
          LogInformation($"[{_gptModel.Name}] Порт {_gptModel.COMPort.PortName} уже открыт, пропускаем инициализацию COM.", isDeviceLog: true);
        }

        // Можно попробовать отправить простую команду-идентификатор (если устройство поддерживает SCPI)
        try
        {
          string idn = await _gptModel.DeviceProtocol.QueryAsync("*IDN?", timeout: 500);
          if (!string.IsNullOrWhiteSpace(idn))
          {
            LogInformation($"[{_gptModel.Name}] Устройство ответило: {idn}", isDeviceLog: true);
          }
        }
        catch (Exception ex)
        {
          LogWarning($"[{_gptModel.Name}] Проверка связи не удалась: {ex.Message}", isDeviceLog: true);
        }

        var answer = await _gptModel.DeviceProtocol.QueryAsync("*IDN?", timeout: 1000);
        if (answer.Contains("GPT"))
          return (true, string.Empty);
        else
          return (false, answer);
      }
      catch (UnauthorizedAccessException ex)
      {
        LogException($"Ошибка доступа к COM-порту {_gptModel?.COMPort?.PortName}", ex, isDeviceLog: true);
        return (false, $"Ошибка доступа к COM-порту {_gptModel?.COMPort?.PortName}: {ex.Message}");
      }
      catch (IOException ex)
      {
        LogException($"Ошибка ввода-вывода при инициализации устройства {_gptModel?.Name}", ex, isDeviceLog: true);
        return (false, $"Ошибка инициализации: {ex.Message}");
      }
      catch (Exception ex)
      {
        LogException($"Ошибка инициализации устройства {_gptModel?.Name}", ex, isDeviceLog: true);
        return (false, $"Ошибка: {ex.Message}");
      }
    }


    /// <inheritdoc />
    public async Task<bool> ResetAsync(IUserMessageService messageService = null)
    {
      return true;
    }
  }
}
