using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Errors.Device;
using Ask.Core.Services.UI;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Device.Emulator;
using Ask.Device.ResponseProcessor.Multimeter.ResponseProcessing;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Device.Runtime.Function.Base.Connected;

/// <summary>
/// Запрашивает идентификацию мультиметра с повтором при отсутствии ответа.
/// </summary>
internal static class MultimeterInitialization
{
  internal static async Task<(bool Connect, string Answer)> InitializeAsync(
    IMultimeter device, string command, int port = 0, IUserInteractionService? messages = null,
    Action<int>? onAttempt = null, Func<int, CancellationToken, Task>? delayAsync = null)
  {
    using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(
      EquipmentExecutionContext.CancellationToken,
      messages?.GetCancellationToken() ?? CancellationToken.None);
    var token = cancellation.Token;
    int attempts = ExecutionConfig.GetIsIdleModeEnabled() ? 1 : 3;
    for (int attempt = 1; attempt <= attempts; attempt++)
    {
      token.ThrowIfCancellationRequested();
      if (attempt > 1)
      {
        int delay = (attempt - 1) * 1000;
        LogInformation($"[{device.Name}] Инициализация: пауза {delay} мс перед попыткой {attempt}/{attempts}.", isDeviceLog: true);
        await (delayAsync ?? Task.Delay)(delay, token);
      }

      LogInformation($"[{device.Name}] Инициализация: попытка {attempt}/{attempts}, команда {command}, ожидание 1000 мс.", isDeviceLog: true);
      onAttempt?.Invoke(attempt);
      try
      {
        string answer = await DeviceProtocolEmulator.QueryMultimeterAsync(
          device, command, $"ASK,{device.Name},0,IDLE", timeout: 1000, port: port,
          cancellationToken: token);
        if (MultimeterResponseProcessor.CheckInitialization(answer))
        {
          LogInformation($"[{device.Name}] Инициализация: попытка {attempt}/{attempts} успешна.", isDeviceLog: true);
          return (true, answer.Trim());
        }
        LogWarning($"[{device.Name}] Инициализация: пустой ответ на попытке {attempt}/{attempts}.", isDeviceLog: true);
      }
      catch (DeviceNoResponseException ex)
      {
        token.ThrowIfCancellationRequested();
        LogWarning($"[{device.Name}] Инициализация: попытка {attempt}/{attempts}: {ex.Message}", isDeviceLog: true);
        if (attempt == attempts)
          throw;
      }
    }

    return (false, $"Нет ответа на команду {command} от {device.Name}");
  }
}
