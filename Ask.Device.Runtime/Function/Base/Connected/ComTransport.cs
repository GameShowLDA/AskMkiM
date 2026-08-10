using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Device.Communication.Com.Extensions;
using Ask.Device.Communication.Common.Threading;
using Ask.Device.Runtime.Base.Device;
using Ask.Device.ResponseProcessor.BreakdownTester.ResponseProcessing;
using Microsoft.Win32.SafeHandles;
using System.IO.Ports;
using System.Reflection;
using System.Runtime.InteropServices;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Device.Runtime.Function.Connected
{
  /// <summary>
  /// Управляет подключением устройства через COM-порт.
  /// </summary>
  internal class ComTransport : IConnectable
  {
    private const int InitializeAttempts = 2;
    private const int InitializeResponseDelay = 50;
    private const int PortReleaseDelay = 200;

    private readonly DeviceWithCOM _device;

    public event Action IsReset;

    /// <summary>
    /// Семафор для синхронизации операций подключения/отключения.
    /// </summary>
    public SemaphoreSlim OperationLock { get; set; } = new SemaphoreSlim(1, 1);

    /// <summary>
    /// Создаёт транспорт для устройства, работающего через COM-порт.
    /// </summary>
    /// <param name="device">Модель устройства с настроенным COM-портом и протоколом обмена.</param>
    public ComTransport(DeviceWithCOM device)
    {
      _device = device ?? throw new ArgumentNullException(nameof(device));
    }

    /// <summary>
    /// Открывает COM-порт на время проверки и выполняет команду инициализации устройства.
    /// </summary>
    /// <param name="userMessageService">Сервис отображения пользовательских сообщений при ошибках открытия порта.</param>
    /// <returns>Результат подключения и текст ошибки, если инициализация не выполнена.</returns>
    public async Task<(bool Connect, string Answer)> InitializeAsync(IUserInteractionService userMessageService = null)
    {
      if (ExecutionConfig.GetIsIdleModeEnabled())
      {
        if (_device is IBreakdownTester)
        {
          return await InitializeCoreAsync();
        }

        return IdleHardwareErrorSimulator.ShouldSimulateHardwareError()
          ? (false, IdleHardwareErrorSimulator.ErrorMessage)
          : (true, string.Empty);
      }

      using (await OperationLock.LockAsync())
      {
        var validation = ValidateConnectionData();
        if (!validation.Connect)
        {
          return validation;
        }

        using (await _device.COMPort.UsePort(_device.Name, userMessageService))
        {
          return await InitializeCoreAsync();
        }
      }
    }

    /// <summary>
    /// Выполняет подключение устройства через стандартную процедуру инициализации.
    /// </summary>
    /// <param name="userMessageService">Сервис отображения пользовательских сообщений при ошибках открытия порта.</param>
    /// <returns>Результат подключения и текст ошибки, если подключение не выполнено.</returns>
    public async Task<(bool Connect, string Answer)> ConnectAsync(IUserInteractionService userMessageService = null)
    {
      return await InitializeAsync(userMessageService);
    }

    /// <summary>
    /// Сбрасывает состояние устройства, закрывает COM-порт и оставляет порт в модели для повторного открытия.
    /// </summary>
    /// <param name="userMessageService">Не используется для COM-отключения.</param>
    /// <returns><c>true</c>, если отключение завершено без критической ошибки; иначе <c>false</c>.</returns>
    public async Task<bool> DisconnectAsync(IUserInteractionService userMessageService = null)
    {
      ResetBreakdownMode();

      if (ExecutionConfig.GetIsIdleModeEnabled())
      {
        if (_device is IBreakdownTester)
        {
          return await SendResetCommandsAsync("отключении устройства");
        }

        return !IdleHardwareErrorSimulator.ShouldSimulateHardwareError();
      }

      using (await OperationLock.LockAsync())
      {
        try
        {
          await ClosePortAsync();
        }
        catch (Exception ex)
        {
          LogException($"Ошибка отключения устройства {_device.Name}", ex, isDeviceLog: true);
          return false;
        }
      }

      await Task.Delay(PortReleaseDelay);

      LogInformation("[DisconnectAsync] COM-порт освобожден. Модель сохранена для повторной инициализации.", isDeviceLog: true);
      return true;
    }

    /// <summary>
    /// Отправляет команды сброса и очистки состояния устройства.
    /// </summary>
    /// <param name="userMessageService">Не используется для COM-сброса.</param>
    /// <returns><c>true</c>, если команды сброса отправлены без ошибки; иначе <c>false</c>.</returns>
    public async Task<bool> ResetAsync(IUserInteractionService userMessageService = null)
    {
      if (ExecutionConfig.GetIsIdleModeEnabled())
      {
        if (_device is IBreakdownTester)
        {
          bool isReset = await SendResetCommandsAsync("сбросе устройства");
          if (isReset)
          {
            IsReset?.Invoke();
          }

          return isReset;
        }

        return !IdleHardwareErrorSimulator.ShouldSimulateHardwareError();
      }

      using (await OperationLock.LockAsync())
      {
        try
        {
          bool isReset = await SendResetCommandsAsync("сбросе устройства");
          if (isReset)
          {
            IsReset?.Invoke();
          }

          return isReset;
        }
        catch (Exception ex)
        {
          LogException($"Ошибка сброса устройства {_device?.Name}", ex, isDeviceLog: true);
          return false;
        }
      }
    }

    /// <summary>
    /// Выполняет команду инициализации и проверяет, что ответ содержит ожидаемый признак устройства.
    /// </summary>
    /// <returns>Результат инициализации и текст ошибки, если устройство не подтвердило подключение.</returns>
    private async Task<(bool Connect, string Answer)> InitializeCoreAsync()
    {
      try
      {
        string answer = string.Empty;

        for (int attempt = 1; attempt <= InitializeAttempts; attempt++)
        {
          answer = await QueryInitializeCommandAsync();
          LogInitializeAnswer(answer);

          if (IsExpectedInitializeAnswer(answer))
          {
            _device.ConnectionInfo.IsConnected = true;
            return (true, string.Empty);
          }
        }

        _device.ConnectionInfo.IsConnected = false;
        return string.IsNullOrEmpty(answer)
          ? (false, "Устройство не ответило на команду инициализации.")
          : (false, $"Неожиданный ответ от устройства: {answer}");
      }
      catch (Exception ex)
      {
        _device.ConnectionInfo.IsConnected = false;
        LogWarning($"[{_device.Name}] Ошибка при опросе команды инициализации: {ex.Message}", isDeviceLog: true);
        return (false, ex.Message);
      }
    }

    /// <summary>
    /// Отправляет команду инициализации из профиля подключения.
    /// </summary>
    /// <returns>Ответ устройства или пустая строка, если ответа нет.</returns>
    private Task<string> QueryInitializeCommandAsync()
    {
      return _device.DeviceProtocol.QueryAsync(
        _device.ConnectedProfile.Initialize,
        responseDelay: InitializeResponseDelay,
        timeout: _device.ConnectedProfile.Timeout);
    }

    /// <summary>
    /// Записывает в лог непустой ответ на команду инициализации.
    /// </summary>
    /// <param name="answer">Ответ устройства.</param>
    private void LogInitializeAnswer(string answer)
    {
      if (string.IsNullOrWhiteSpace(answer))
      {
        return;
      }

      LogInformation($"[{_device.Name}] Ответ на {_device.ConnectedProfile.Initialize}: {answer}", isDeviceLog: true);
    }

    /// <summary>
    /// Проверяет, содержит ли ответ ожидаемый признак устройства.
    /// </summary>
    /// <param name="answer">Ответ устройства на команду инициализации.</param>
    /// <returns><c>true</c>, если ответ соответствует профилю подключения.</returns>
    private bool IsExpectedInitializeAnswer(string answer)
    {
      if (_device is IBreakdownTester)
      {
        return BreakdownTesterResponseProcessor.CheckInitialization(
          answer,
          _device.ConnectedProfile.CheckMode);
      }

      return !string.IsNullOrWhiteSpace(answer)
        && answer.Contains(_device.ConnectedProfile.CheckMode);
    }

    /// <summary>
    /// Сбрасывает режим пробойной установки, если устройство поддерживает такой режим.
    /// </summary>
    private void ResetBreakdownMode()
    {
      if (_device is IBreakdownTester breakdownTester)
      {
        breakdownTester.Mode = BreakdownTypeMode.None;
      }
    }

    /// <summary>
    /// Закрывает COM-порт с предварительным сбросом устройства и отменой ожидающих операций ввода-вывода.
    /// </summary>
    private async Task ClosePortAsync()
    {
      var port = _device.COMPort;
      if (port == null)
      {
        LogWarning($"[{_device.Name}] COM-порт не задан.", isDeviceLog: true);
        return;
      }

      string portName = port.PortName;

      if (!port.IsOpen)
      {
        LogInformation($"[{_device.Name}] COM-порт {portName} уже закрыт.", isDeviceLog: true);
        return;
      }

      await SendResetCommandsAsync("сбросе перед отключением");
      CancelPendingIo(port);
      ClosePort(port);

      _device.ConnectionInfo.IsConnected = false;
      LogInformation($"[{_device.Name}] COM-порт {portName} оставлен в модели для повторного открытия.", isDeviceLog: true);
    }

    /// <summary>
    /// Отправляет устройству команды сброса и очистки из профиля подключения.
    /// </summary>
    /// <param name="operationName">Название операции для сообщения в лог.</param>
    /// <returns><c>true</c>, если обе команды отправлены без ошибки; иначе <c>false</c>.</returns>
    private async Task<bool> SendResetCommandsAsync(string operationName)
    {
      try
      {
        await _device.DeviceProtocol.QueryAsync(_device.ConnectedProfile.Reset);
        await _device.DeviceProtocol.QueryAsync(_device.ConnectedProfile.Clear);
        LogInformation($"[{_device.Name}] Отправлены команды сброса и очистки.", isDeviceLog: true);
        return true;
      }
      catch (Exception ex)
      {
        LogWarning($"[{_device.Name}] Ошибка при {operationName}: {ex.Message}", isDeviceLog: true);
        return false;
      }
    }

    /// <summary>
    /// Отменяет незавершённые операции ввода-вывода порта через Windows API.
    /// </summary>
    /// <param name="port">Открытый COM-порт.</param>
    private void CancelPendingIo(SerialPort port)
    {
      try
      {
        var handle = GetSafeHandle(port);
        if (handle == null || handle.IsInvalid)
        {
          return;
        }

        CancelIoEx(handle, IntPtr.Zero);
        LogInformation($"[{_device.Name}] CancelIoEx вызван для {port.PortName}.", isDeviceLog: true);
      }
      catch (Exception ex)
      {
        LogWarning($"[{_device.Name}] Ошибка CancelIoEx: {ex.Message}", isDeviceLog: true);
      }
    }

    /// <summary>
    /// Закрывает COM-порт штатным методом <see cref="SerialPort.Close"/>.
    /// </summary>
    /// <param name="port">COM-порт, который нужно закрыть.</param>
    private void ClosePort(SerialPort port)
    {
      try
      {
        port.Close();
        LogInformation($"[{_device.Name}] COM-порт {port.PortName} закрыт.", isDeviceLog: true);
      }
      catch (Exception ex)
      {
        LogWarning($"[{_device.Name}] Ошибка при Close(): {ex.Message}", isDeviceLog: true);
      }
    }

    /// <summary>
    /// Проверяет, заданы ли COM-порт, протокол и профиль подключения устройства.
    /// </summary>
    /// <returns>
    /// Кортеж: <c>true</c>, если данные подключения заданы;
    /// иначе <c>false</c> и сообщение об ошибке.
    /// </returns>
    private (bool Connect, string Answer) ValidateConnectionData()
    {
      if (_device.COMPort == null)
      {
        var msg = $"[{_device.Name}] COM-порт не инициализирован.";
        LogWarning(msg, isDeviceLog: true);
        return (false, msg);
      }

      if (_device.DeviceProtocol == null)
      {
        var msg = $"[{_device.Name}] Протокол устройства не инициализирован.";
        LogWarning(msg, isDeviceLog: true);
        return (false, msg);
      }

      if (_device.ConnectedProfile == null)
      {
        var msg = $"[{_device.Name}] Профиль COM-подключения не инициализирован.";
        LogWarning(msg, isDeviceLog: true);
        return (false, msg);
      }

      var message = $"[{_device.Name}] Данные инициализированы: COM-порт и протокол доступны.";
      LogInformation(message, isDeviceLog: true);
      return (true, message);
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

    /// <summary>
    /// Отменяет ожидающие операции ввода-вывода для файлового дескриптора Windows.
    /// </summary>
    /// <param name="hFile">Дескриптор COM-порта.</param>
    /// <param name="lpOverlapped">Указатель на конкретную overlapped-операцию или <see cref="IntPtr.Zero"/> для всех операций.</param>
    /// <returns><c>true</c>, если операция Windows API выполнена успешно; иначе <c>false</c>.</returns>
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CancelIoEx(SafeFileHandle hFile, IntPtr lpOverlapped);
  }
}
