using NewCore.Base.Device;
using NewCore.Device;
using NewCore.Function.GPT.Data;
using System;
using System.IO;
using System.IO.Ports;
using System.Threading.Tasks;
using Utilities.Interface;
using static Utilities.LoggerUtility;

namespace NewCore.Function.GPT
{
  /// <summary>
  /// Класс для управления состоянием пробойной установки GPT79904.
  /// </summary>
  public class ConnectableManager : IConnectable
  {
    private GPT79904 _gptModel;

    /// <summary>
    /// Создаёт новый экземпляр <see cref="ConnectableManager"/>.
    /// </summary>
    /// <param name="gpt79904">Модель устройства GPT79904.</param>
    public ConnectableManager(GPT79904 gpt79904)
    {
      _gptModel = gpt79904 ?? throw new ArgumentNullException(nameof(gpt79904));
    }

    public async Task<(bool Connect, string Answer)> ConnectAsync(IUserMessageService _ = null)
    {
      if (_gptModel?.DeviceProtocol?.OperationLock == null)
        return (false, "DeviceProtocol не инициализирован");

      try
      {
        if (_gptModel.COMPort == null)
          return (false, "COM-порт не инициализирован");


        try
        {
          if (_gptModel.COMPort.IsOpen)
          {
            _gptModel.COMPort.Close();
          }

          _gptModel.COMPort.Dispose(); // ← Это важно
        }
        catch (Exception ex)
        {
          LogWarning($"[{_gptModel.Name}] Ошибка при попытке очистки COM перед повторным открытием: {ex.Message}");
        }

        _gptModel.COMPort.DtrEnable = true;
        _gptModel.COMPort.RtsEnable = true;
        _gptModel.COMPort.NewLine = "\r\n";
        _gptModel.COMPort.ReadTimeout = 2000;
        _gptModel.COMPort.WriteTimeout = 2000;

        _gptModel.COMPort.Open();
        LogInformation($"[{_gptModel.Name}] Порт {_gptModel.COMPort.PortName} открыт.", isDeviceLog: true);

        await InitializeAsync();

        return (true, "");
      }
      catch (UnauthorizedAccessException ex)
      {
        LogException($"Ошибка доступа к {_gptModel.COMPort?.PortName}: {ex.Message}", ex, isDeviceLog: true);
        return (false, $"Нет доступа к порту {_gptModel.COMPort?.PortName}: {ex.Message}");
      }
      catch (IOException ex)
      {
        LogException($"Ошибка ввода/вывода при открытии порта {_gptModel.COMPort?.PortName}: {ex.Message}", ex, isDeviceLog: true);
        return (false, $"Ошибка ввода/вывода: {ex.Message}");
      }
      catch (Exception ex)
      {
        LogException($"Ошибка открытия порта {_gptModel.COMPort?.PortName}: {ex.Message}", ex, isDeviceLog: true);
        return (false, $"Ошибка: {ex.Message}");
      }
    }

    public async Task<bool> DisconnectAsync(IUserMessageService _ = null)
    {
      try
      {
        if (_gptModel?.COMPort?.IsOpen == true)
        {
          LogInformation($"[{_gptModel.Name}] Закрываю порт {_gptModel.COMPort.PortName}", isDeviceLog: true);
          await _gptModel.DeviceProtocol.QueryAsync("*RST");
          await _gptModel.DeviceProtocol.QueryAsync("*CLS");
          _gptModel.COMPort.Close();
        }

        return true;
      }
      catch (Exception ex)
      {
        LogException($"Ошибка при отключении {_gptModel?.COMPort?.PortName}: {ex.Message}", ex, isDeviceLog: true);
        return false;
      }
    }

    public async Task<(bool Connect, string Answer)> InitializeAsync(IUserMessageService messageService = null)
    {
      try
      {
        if (_gptModel == null || _gptModel.COMPort == null)
          return (false, "Устройство или COM-порт не инициализирован");

        if (!_gptModel.COMPort.IsOpen)
        {
          try
          {
            _gptModel.COMPort.Open();
            LogInformation($"[{_gptModel.Name}] COM-порт {_gptModel.COMPort.PortName} открыт в InitializeAsync()", isDeviceLog: true);
          }
          catch (Exception ex)
          {
            LogWarning($"[{_gptModel.Name}] COM-порт не открылся с первого раза. Пробуем повторно: {ex.Message}", isDeviceLog: true);

            try
            {
              await DisconnectAsync();
              _gptModel.COMPort.Open();
              LogInformation($"[{_gptModel.Name}] Повторное открытие COM-порта {_gptModel.COMPort.PortName} успешно", isDeviceLog: true);
            }
            catch (Exception retryEx)
            {
              LogException($"[{_gptModel.Name}] Повторное открытие COM-порта не удалось: {retryEx.Message}", retryEx, isDeviceLog: true);
              return (false, $"Не удалось открыть COM-порт {_gptModel.COMPort.PortName}: {retryEx.Message}");
            }
          }
        }

        try
        {
          string idn = await _gptModel.DeviceProtocol.QueryAsync("*IDN?", timeout: 500);
          if (!string.IsNullOrWhiteSpace(idn))
          {
            LogInformation($"[{_gptModel.Name}] Ответ на *IDN?: {idn}", isDeviceLog: true);
          }
        }
        catch (Exception ex)
        {
          LogWarning($"[{_gptModel.Name}] Ошибка при опросе *IDN?: {ex.Message}", isDeviceLog: true);
        }

        string answer = await _gptModel.DeviceProtocol.QueryAsync("*IDN?", timeout: 1000);
        if (answer.Contains("GPT"))
          return (true, string.Empty);
        else
          return (false, $"Неожиданный ответ от устройства: {answer}");
      }
      catch (UnauthorizedAccessException ex)
      {
        LogException($"Ошибка доступа к COM-порту {_gptModel?.COMPort?.PortName}: {ex.Message}", ex, isDeviceLog: true);
        return (false, $"Доступ к COM-порту запрещён: {ex.Message}");
      }
      catch (IOException ex)
      {
        LogException($"Ошибка I/O при инициализации {_gptModel?.Name}: {ex.Message}", ex, isDeviceLog: true);
        return (false, $"Ошибка ввода/вывода: {ex.Message}");
      }
      catch (Exception ex)
      {
        LogException($"Общая ошибка инициализации {_gptModel?.Name}: {ex.Message}", ex, isDeviceLog: true);
        return (false, $"Ошибка инициализации: {ex.Message}");
      }
    }

    public async Task<bool> ResetAsync(IUserMessageService messageService = null)
    {
      try
      {
        await _gptModel.DeviceProtocol.QueryAsync("*RST");
        await _gptModel.DeviceProtocol.QueryAsync("*CLS");
        return true;
      }
      catch (Exception ex)
      {
        LogException($"Ошибка сброса устройства {_gptModel?.Name}", ex, isDeviceLog: true);
        return false;
      }
    }
  }
}
