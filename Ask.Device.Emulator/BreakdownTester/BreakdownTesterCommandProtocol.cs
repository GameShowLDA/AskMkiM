using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Device.Emulator.BreakdownTester;

/// <summary>
/// Выбирает реальный или эмулируемый протокол ППУ и записывает команды и ответы в журнал.
/// </summary>
internal sealed class BreakdownTesterCommandProtocol : IDeviceProtocol
{
  private readonly IBreakdownTester _device;
  private readonly IDeviceProtocol _protocol;

  public BreakdownTesterCommandProtocol(IBreakdownTester device, IDeviceProtocol realProtocol)
  {
    _device = device;
    _protocol = new Protocols.ModeSelectingDeviceProtocol(
      () => realProtocol,
      new BreakdownTesterEmulatorProtocol(
        () => IdleHardwareErrorSimulator.ShouldSimulateHardwareError(_device)));
  }

  /// <inheritdoc />
  public SemaphoreSlim OperationLock { get; set; } = new(1, 1);

  /// <inheritdoc />
  public async Task<string> QueryAsync(
    string command,
    double responseDelay = 0,
    int timeout = 0,
    int port = 0,
    int delayBeforeCall = 0,
    CancellationToken cancellationToken = default)
  {
    string mode = ExecutionConfig.GetIsIdleModeEnabled() ? "Холостой режим" : "Реальное обращение";
    string name = $"{_device.Name}({_device.NumberChassis}.{_device.Number})";
    LogInformation($"{mode} | [{name}] Команда ППУ: \"{command}\".", isDeviceLog: true);

    string response = await _protocol.QueryAsync(
      command,
      responseDelay,
      timeout,
      port,
      delayBeforeCall,
      cancellationToken);

    LogInformation(
      $"{mode} | [{name}] Ответ ППУ на \"{command}\": \"{(string.IsNullOrEmpty(response) ? "<пустой>" : response)}\".",
      isDeviceLog: true);
    return response;
  }
}
