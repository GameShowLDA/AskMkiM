using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Errors.Device;
using Ask.Core.Services.UI;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Device.Emulator;
using Ask.Device.ResponseProcessor.Multimeter.ResponseProcessing;
using Ask.Device.Runtime.Base.Device;
using Ask.Core.Shared.Metadata.Commands.MultimeterCommands.Connected;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Device.Runtime.Function.Base.Multimeter.Measurements.Common;

/// <summary>
/// Повторяет запрос результата мультиметра и проверяет связь перед восстановлением измерения.
/// </summary>
internal static class MultimeterMeasurementQuery
{
  internal static async Task<(string Response, int Attempt, int MaxAttempts)> QueryAsync(
    IMultimeter device, string command, string idleResponse, string operation,
    int timeout, double responseDelay = 0, IUserInteractionService? messages = null,
    CancellationToken cancellationToken = default)
  {
    using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(
      cancellationToken, EquipmentExecutionContext.CancellationToken,
      messages?.GetCancellationToken() ?? CancellationToken.None);
    var token = cancellation.Token;
    int maxAttempts = ExecutionConfig.GetIsIdleModeEnabled() ? 1 : 2;
    bool recovered = false;
    for (int attempt = 1; attempt <= maxAttempts; attempt++)
    {
      token.ThrowIfCancellationRequested();
      LogInformation($"[{device.Name}] {operation}: запрос {attempt}/{maxAttempts}, восстановление={recovered}, ожидание {timeout} мс.", isDeviceLog: true);
      try
      {
        string response = await DeviceProtocolEmulator.QueryMultimeterAsync(
          device, command, idleResponse, responseDelay: responseDelay, timeout: timeout,
          cancellationToken: token);
        token.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(response) && !ExecutionConfig.GetIsIdleModeEnabled())
          throw new DeviceNoResponseException(device, command, timeout);
        return (response, recovered ? attempt + 2 : attempt, recovered ? 4 : maxAttempts);
      }
      catch (DeviceNoResponseException ex)
      {
        token.ThrowIfCancellationRequested();
        int displayAttempt = recovered ? attempt + 2 : attempt;
        int displayMaximum = recovered ? 4 : maxAttempts;
        ex.Operation = operation + (displayAttempt > 1 ? $" (Попытка {displayAttempt}/{displayMaximum})" : string.Empty);
        LogWarning($"[{device.Name}] {ex.Operation}: {ex.Message}", isDeviceLog: true);
        if (attempt < maxAttempts)
          continue;
        if (recovered || ExecutionConfig.GetIsIdleModeEnabled())
          throw;

        ConnectedBaseProfile profile = device switch
        {
          DeviceWithIP ip => ip.ConnectedProfile,
          DeviceWithUSB usb => usb.ConnectedProfile,
          DeviceWithCOM com => com.ConnectedProfile,
          _ => throw new NotSupportedException($"Неизвестный профиль мультиметра {device.GetType().Name}."),
        };
        LogInformation($"[{device.Name}] {operation}: один скрытый запрос {profile.Initialize} для проверки связи.", isDeviceLog: true);
        string identification;
        try
        {
          identification = await DeviceProtocolEmulator.QueryMultimeterAsync(
            device, profile.Initialize, $"ASK,{device.Name},0,IDLE", timeout: 1000,
            cancellationToken: token);
        }
        catch (DeviceNoResponseException probeError)
        {
          probeError.Operation = ex.Operation;
          throw;
        }
        if (!MultimeterResponseProcessor.CheckInitialization(identification))
          throw;

        recovered = true;
        maxAttempts = 2;
        attempt = 0;
        LogInformation($"[{device.Name}] {operation}: связь подтверждена, разрешены ещё два запроса результата.", isDeviceLog: true);
      }
    }
    throw new InvalidOperationException("Не удалось получить результат мультиметра.");
  }
}
