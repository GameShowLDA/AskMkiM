using Ask.Core.Shared.Interfaces.DeviceInterfaces;

namespace Ask.Device.Emulator.Multimeter;

/// <summary>
/// Возвращает заданный ответ на команду мультиметра в холостом режиме.
/// </summary>
internal sealed class MultimeterEmulatorProtocol : IDeviceProtocol
{
  private readonly string _response;
  private readonly Func<bool> _hardwareErrorProvider;

  public MultimeterEmulatorProtocol(string response)
    : this(response, () => false)
  {
  }

  internal MultimeterEmulatorProtocol(string response, Func<bool> hardwareErrorProvider)
  {
    _response = response;
    _hardwareErrorProvider = hardwareErrorProvider
      ?? throw new ArgumentNullException(nameof(hardwareErrorProvider));
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
    string response = _hardwareErrorProvider() && !IsMeasurementCommand(command)
      ? string.Empty
      : _response;
    return Task.FromResult(response);
  }

  private static bool IsMeasurementCommand(string command)
  {
    string normalized = command.Trim();
    return normalized.Equals("READ?", StringComparison.OrdinalIgnoreCase)
      || normalized.StartsWith("MEAS", StringComparison.OrdinalIgnoreCase)
      || normalized.StartsWith("FETC", StringComparison.OrdinalIgnoreCase)
      || normalized.StartsWith("FETCH", StringComparison.OrdinalIgnoreCase);
  }
}
