using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;

namespace Ask.Device.Emulator.Multimeter;

/// <summary>
/// Возвращает заданный ответ на команду мультиметра в холостом режиме.
/// </summary>
internal sealed class MultimeterEmulatorProtocol : IDeviceProtocol
{
  private readonly string _response;

  public MultimeterEmulatorProtocol(string response)
  {
    _response = response;
  }

  /// <inheritdoc />
  public SemaphoreSlim OperationLock { get; set; } = new(1, 1);

  /// <inheritdoc />
  public Task<string> QueryAsync(
    string command,
    double responseDelay = 0,
    int timeout = 0,
    int port = 0,
    int delayBeforeCall = 0,
    CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    string response = IdleHardwareErrorSimulator.ShouldSimulateHardwareError()
      ? string.Empty
      : _response;
    return Task.FromResult(response);
  }
}
