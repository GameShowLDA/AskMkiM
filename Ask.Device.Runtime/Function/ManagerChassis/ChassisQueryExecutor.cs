using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Chassis;
using Ask.Device.Emulator;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Device.Runtime.Function.ManagerChassis
{
  /// <summary>
  /// Выполняет команды шасси через реальный протокол или эмулятор и записывает обмен в журнал.
  /// </summary>
  internal sealed class ChassisQueryExecutor
  {
    private readonly IChassisManager _chassis;
    private readonly IDeviceProtocol _protocol;

    public ChassisQueryExecutor(IChassisManager chassis)
    {
      _chassis = chassis;
      _protocol = DeviceProtocolEmulator.CreateChassis(chassis);
    }

    public async Task<string> QueryAsync(
      string command,
      int timeout = 1000,
      CancellationToken cancellationToken = default)
    {
      string mode = ExecutionConfig.GetIsIdleModeEnabled() ? "Холостой режим" : "Реальное обращение";
      string device = $"{_chassis.Name}({_chassis.Number})";
      LogInformation($"{mode} | [{device}] Команда шасси: \"{command}\".", isDeviceLog: true);

      string response = await _protocol.QueryAsync(
        command,
        timeout: timeout,
        cancellationToken: cancellationToken);

      LogInformation(
        $"{mode} | [{device}] Ответ шасси на \"{command}\": \"{(string.IsNullOrEmpty(response) ? "<пустой>" : response)}\".",
        isDeviceLog: true);
      return response;
    }
  }
}
