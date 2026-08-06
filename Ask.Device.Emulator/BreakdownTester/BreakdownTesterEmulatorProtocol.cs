using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using System.Collections.Concurrent;

namespace Ask.Device.Emulator.BreakdownTester;

/// <summary>
/// Эмулирует SCPI-протокол пробойной установки GPT79904.
/// </summary>
internal sealed class BreakdownTesterEmulatorProtocol : IDeviceProtocol
{
  private readonly Func<bool> _hardwareErrorProvider;
  private readonly ConcurrentDictionary<string, string> _values = new(StringComparer.OrdinalIgnoreCase);

  public BreakdownTesterEmulatorProtocol(Func<bool>? hardwareErrorProvider = null)
  {
    _hardwareErrorProvider = hardwareErrorProvider ?? (() => false);
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
    if (_hardwareErrorProvider())
    {
      return Task.FromResult(string.Empty);
    }

    string normalized = command.Trim();
    if (normalized.Equals("*IDN?", StringComparison.OrdinalIgnoreCase))
    {
      return Task.FromResult("GW INSTEK,GPT-79904,IDLE,1.0");
    }

    if (normalized.EndsWith('?'))
    {
      string key = normalized.TrimEnd('?').Trim();
      if (key.Equals("MEAS", StringComparison.OrdinalIgnoreCase))
      {
        return Task.FromResult("PASS,0,0,1.000mA");
      }

      if (key.Equals("FUNC:TEST", StringComparison.OrdinalIgnoreCase))
      {
        return Task.FromResult($"TEST {_values.GetValueOrDefault(key, "OFF")}");
      }

      return Task.FromResult(_values.GetValueOrDefault(key, "0"));
    }

    int separator = normalized.LastIndexOf(' ');
    if (separator > 0)
    {
      _values[normalized[..separator].Trim()] = normalized[(separator + 1)..].Trim();
    }

    return Task.FromResult(string.Empty);
  }
}
