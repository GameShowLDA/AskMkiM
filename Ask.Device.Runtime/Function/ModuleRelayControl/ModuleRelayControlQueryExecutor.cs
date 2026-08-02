using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule;
using Ask.Device.Emulator;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Device.Runtime.Function.ModuleRelayControl
{
  /// <summary>
  /// Выполняет команды МКР через реальный протокол или эмулятор и записывает обмен в журнал.
  /// </summary>
  internal sealed class ModuleRelayControlQueryExecutor
  {
    private readonly IRelaySwitchModule _module;
    private readonly IDeviceProtocol _protocol;

    public ModuleRelayControlQueryExecutor(IRelaySwitchModule module)
    {
      _module = module;
      _protocol = DeviceProtocolEmulator.CreateModuleRelayControl(module);
    }

    public async Task<string> QueryAsync(
      string command,
      int timeout,
      CancellationToken cancellationToken = default)
    {
      string mode = ExecutionConfig.GetIsIdleModeEnabled()
        ? "Холостой режим"
        : "Реальное обращение";
      string device = $"{_module.Name}({_module.NumberChassis}.{_module.Number})";

      LogInformation($"{mode} | [{device}] Команда МКР: \"{command}\".", isDeviceLog: true);

      string response = await _protocol.QueryAsync(
        command,
        timeout: timeout,
        cancellationToken: cancellationToken);

      LogInformation(
        $"{mode} | [{device}] Ответ МКР на \"{command}\": \"{(string.IsNullOrEmpty(response) ? "<пустой>" : response)}\".",
        isDeviceLog: true);

      return response;
    }
  }
}
