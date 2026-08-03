using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Device.Runtime.Function.Base.Multimeter
{
  /// <summary>
  /// Выполняет команды мультиметра через реальный протокол или эмулируемый ответ и записывает обмен в журнал.
  /// </summary>
  internal static class MultimeterQueryExecutor
  {
    public static async Task<string> QueryAsync(
      IMultimeter device,
      string command,
      string idleResponse,
      double responseDelay = 0,
      int timeout = 0,
      int port = 0,
      CancellationToken cancellationToken = default)
    {
      string mode = ExecutionConfig.GetIsIdleModeEnabled() ? "Холостой режим" : "Реальное обращение";
      string name = $"{device.Name}({device.NumberChassis}.{device.Number})";
      LogInformation($"{mode} | [{name}] Команда мультиметра: \"{command}\".", isDeviceLog: true);

      string response;
      if (ExecutionConfig.GetIsIdleModeEnabled())
      {
        response = IdleHardwareErrorSimulator.ShouldSimulateHardwareError()
          ? string.Empty
          : idleResponse;
      }
      else
      {
        response = await device.DeviceProtocol.QueryAsync(
          command,
          responseDelay: responseDelay,
          timeout: timeout,
          port: port,
          cancellationToken: cancellationToken);
      }

      LogInformation(
        $"{mode} | [{name}] Ответ мультиметра на \"{command}\": \"{(string.IsNullOrEmpty(response) ? "<пустой>" : response)}\".",
        isDeviceLog: true);
      return response;
    }
  }
}
