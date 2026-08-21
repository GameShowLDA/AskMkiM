using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice;
using Ask.Device.Emulator;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Device.Runtime.AskMkiM.Function.DeviceBusCommutation
{
  /// <summary>
  /// Выполняет команды УКШ через реальный протокол или эмулятор и записывает обмен в журнал.
  /// </summary>
  internal sealed class DeviceBusCommutationQueryExecutor
  {
    private readonly ISwitchingDevice device;
    private readonly IDeviceProtocol protocol;

    public DeviceBusCommutationQueryExecutor(ISwitchingDevice device)
    {
      this.device = device;
      protocol = DeviceProtocolEmulator.CreateDeviceBusCommutation(device);
    }

    public async Task<string> QueryAsync(string command, int timeout = 1000, CancellationToken cancellationToken = default)
    {
      string mode = ExecutionConfig.GetIsIdleModeEnabled() ? "Холостой режим" : "Реальное обращение";
      string name = $"{device.Name}({device.NumberChassis}.{device.Number})";
      LogInformation($"{mode} | [{name}] Команда УКШ: \"{command}\".", isDeviceLog: true);
      string response = await protocol.QueryAsync(command, timeout: timeout, cancellationToken: cancellationToken);
      LogInformation($"{mode} | [{name}] Ответ УКШ на \"{command}\": \"{(string.IsNullOrEmpty(response) ? "<пустой>" : response)}\".", isDeviceLog: true);
      return response;
    }
  }
}


