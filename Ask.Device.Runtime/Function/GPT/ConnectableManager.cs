using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Device.Communication.Com;
using Ask.Device.Communication.Common;
using Microsoft.Win32.SafeHandles;
using NewCore.Device;
using System.IO.Ports;
using System.Reflection;
using System.Runtime.InteropServices;
using static Ask.LogLib.LoggerUtility;

namespace NewCore.Function.GPT
{
  /// <summary>
  /// Класс для управления подключением и состоянием пробойной установки GPT79904.
  /// Реализует интерфейс <see cref="IConnectable"/>.
  /// </summary>
  public class ConnectableManager : IConnectable
  {
    private GPT79904 _gptModel;

    public event Action IsReset;

    /// <summary>
    /// Семафор для синхронизации операций подключения/отключения.
    /// </summary>
    public SemaphoreSlim OperationLock { get; set; } = new SemaphoreSlim(1, 1);

    /// <summary>
    /// Создаёт новый экземпляр <see cref="ConnectableManager"/>.
    /// </summary>
    /// <param name="gpt79904">Модель устройства GPT79904.</param>
    public ConnectableManager(GPT79904 gpt79904)
    {
      _gptModel = gpt79904 ?? throw new ArgumentNullException(nameof(gpt79904));
    }

    /// <summary>
    /// Асинхронно подключается к устройству GPT79904 через COM-порт.
    /// </summary>
    /// <param name="messageService">Опциональный сервис вывода сообщений пользователю.</param>
    /// <returns>Кортеж: <c>true</c>, если подключение выполнено успешно; строка с текстом ошибки или пустая строка.</returns>
    public async Task<(bool Connect, string Answer)> ConnectAsync(IUserInteractionService messageService = null)
    {
      return await InitializeAsync(messageService);
    }

    /// <summary>
    /// Асинхронно отключается от устройства GPT79904, освобождает COM-порт и уничтожает модель.
    /// </summary>
    public async Task<bool> DisconnectAsync(IUserInteractionService _ = null)
    {
      _gptModel.Mode = BreakdownTypeMode.None;
      if (ExecutionConfig.GetIsIdleModeEnabled())
      {
        return true;
      }

      using (await OperationLock.LockAsync())
      {
        try
        {
          if (_gptModel?.COMPort != null)
          {
            string portName = _gptModel.COMPort.PortName;

            try
            {
              if (_gptModel.COMPort.IsOpen)
              {
                try
                {
                  await _gptModel.DeviceProtocol.QueryAsync("*RST");
                  await _gptModel.DeviceProtocol.QueryAsync("*CLS");
                  LogInformation($"[{_gptModel.Name}] Отправлены команды сброса перед закрытием.", isDeviceLog: true);
                }
                catch (Exception ex)
                {
                  LogWarning($"[{_gptModel.Name}] Ошибка при сбросе перед отключением: {ex.Message}", isDeviceLog: true);
                }

                try
                {
                  var handle = GetSafeHandle(_gptModel.COMPort);
                  if (handle != null && !handle.IsInvalid)
                  {
                    CancelIoEx(handle, IntPtr.Zero);
                    LogInformation($"[{_gptModel.Name}] CancelIoEx вызван для {portName}", isDeviceLog: true);
                  }
                }
                catch (Exception ex)
                {
                  LogWarning($"[{_gptModel.Name}] Ошибка CancelIoEx: {ex.Message}", isDeviceLog: true);
                }

                try
                {
                  _gptModel.COMPort.Close();
                  LogInformation($"[{_gptModel.Name}] COM-порт {portName} закрыт.", isDeviceLog: true);
                }
                catch (Exception ex)
                {
                  LogWarning($"[{_gptModel.Name}] Ошибка при Close(): {ex.Message}", isDeviceLog: true);
                }
              }
            }
            catch (Exception ex)
            {
              LogWarning($"[{_gptModel.Name}] Ошибка при обработке COM-порта: {ex.Message}", isDeviceLog: true);
            }

            try
            {
              _gptModel.COMPort.Dispose();
              LogInformation($"[{_gptModel.Name}] COM-порт {portName} уничтожен (Dispose).", isDeviceLog: true);
            }
            catch (Exception ex)
            {
              LogWarning($"[{_gptModel.Name}] Ошибка при Dispose(): {ex.Message}", isDeviceLog: true);
            }

            _gptModel.DeviceProtocol = null;
            _gptModel.COMPort = null;
          }
        }
        catch (Exception ex)
        {
          LogException($"Ошибка отключения устройства {_gptModel?.Name}", ex, isDeviceLog: true);
          return false;
        }
      }

      _gptModel = null;
      GC.Collect();
      GC.WaitForPendingFinalizers();
      Task.Delay(1000).GetAwaiter().GetResult();

      LogInformation("[DisconnectAsync] Устройство уничтожено, COM-порт освобожден.", isDeviceLog: true);
      return true;
    }

    /// <summary>
    /// Асинхронно инициализирует устройство GPT79904.
    /// Выполняет проверку COM-порта и опрос команды *IDN?.
    /// </summary>
    public async Task<(bool Connect, string Answer)> InitializeAsync(IUserInteractionService messageService = null)
    {
      if (ExecutionConfig.GetIsIdleModeEnabled())
      {
        return (true, string.Empty);
      }

      using (await OperationLock.LockAsync())
      {
        using (await _gptModel.COMPort.UsePort(_gptModel.Name, messageService))
        {
          var isValid = CheckData();
          if (!isValid.Connect)
          {
            return isValid;
          }

          try
          {
            string idn = string.Empty;
            for (int i = 0; i < 2; i++)
            {
              idn = await _gptModel.DeviceProtocol.QueryAsync("*IDN?", responseDelay: 50, timeout: 1000);
              if (!string.IsNullOrWhiteSpace(idn))
              {
                LogInformation($"[{_gptModel.Name}] Ответ на *IDN?: {idn}", isDeviceLog: true);
              }

              if (idn.Contains("GPT"))
              {
                return (true, string.Empty);
              }
            }

            return string.IsNullOrEmpty(idn)
              ? (false, "Устройство не ответило на команду инициализации.")
              : (false, $"Неожиданный ответ от устройства: {idn}");
          }
          catch (Exception ex)
          {
            LogWarning($"[{_gptModel.Name}] Ошибка при опросе *IDN?: {ex.Message}", isDeviceLog: true);
            return (false, ex.Message);
          }
        }
      }
    }

    /// <summary>
    /// Асинхронно выполняет сброс устройства GPT79904 (*RST, *CLS).
    /// </summary>
    public async Task<bool> ResetAsync(IUserInteractionService messageService = null)
    {
      if (ExecutionConfig.GetIsIdleModeEnabled())
      {
        return true;
      }

      using (await OperationLock.LockAsync())
      {
        try
        {
          await _gptModel.DeviceProtocol.QueryAsync("*RST");
          await _gptModel.DeviceProtocol.QueryAsync("*CLS");
          IsReset?.Invoke();
          return true;
        }
        catch (Exception ex)
        {
          LogException($"Ошибка сброса устройства {_gptModel?.Name}", ex, isDeviceLog: true);
          return false;
        }
      }
    }

    /// <summary>
    /// Возвращает строку состояния подключения и активной конфигурации устройства.
    /// </summary>
    /// <returns>Текущее состояние подключения.</returns>
    public string GetConnectionStatus()
    {
      return GetConnectionStatusAsync().GetAwaiter().GetResult();
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CancelIoEx(SafeFileHandle hFile, IntPtr lpOverlapped);

    /// <summary>
    /// Проверяет, инициализированы ли COM-порт и протокол устройства.
    /// </summary>
    /// <returns>
    /// Кортеж: <c>true</c>, если COM-порт и протокол устройства заданы;
    /// иначе <c>false</c> и сообщение об ошибке.
    /// </returns>
    private (bool Connect, string Answer) CheckData()
    {
      bool isValid = _gptModel.COMPort != null && _gptModel.DeviceProtocol != null;
      string msg;

      if (isValid)
      {
        msg = $"[{_gptModel.Name}] Данные инициализированы: COM-порт и протокол доступны.";
        LogInformation(msg, isDeviceLog: true);
      }
      else
      {
        msg = $"[{_gptModel.Name}] COM-порт или протокол устройства не инициализированы.";
        LogWarning(msg, isDeviceLog: true);
      }

      return (isValid, msg);
    }

    /// <summary>
    /// Получает строковое представление текущего состояния устройства.
    /// </summary>
    /// <returns>Строка состояния для активного режима.</returns>
    private async Task<string> GetConnectionStatusAsync()
    {
      switch (_gptModel.Mode)
      {
        case BreakdownTypeMode.ACW:
          return _gptModel.AcwManger.Config.GetConfigurationAsTextAsync().Result;

        case BreakdownTypeMode.DCW:
          return _gptModel.DcwManger.Config.GetConfigurationAsTextAsync().Result;

        case BreakdownTypeMode.IR:
          return _gptModel.IrManger.Config.GetConfigurationAsTextAsync().Result;

        default:
          return "Режим не определён";
      }
    }

    /// <summary>
    /// Извлекает безопасный дескриптор открытого COM-порта через рефлексию.
    /// </summary>
    /// <param name="port">Последовательный порт устройства.</param>
    /// <returns>Безопасный дескриптор потока порта либо <see langword="null"/>.</returns>
    private SafeFileHandle GetSafeHandle(SerialPort port)
    {
      var baseStream = port.BaseStream;
      var field = baseStream.GetType().GetField("_handle", BindingFlags.NonPublic | BindingFlags.Instance);
      return field?.GetValue(baseStream) as SafeFileHandle;
    }
  }
}
