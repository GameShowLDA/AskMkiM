using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Errors.Device;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.PowerSupply;
using System.Globalization;

namespace Ask.Device.Emulator.PowerSupply;

/// <summary>
/// Эмулирует команды источника Б5-108 без подключения нагрузки и обращения к сети.
/// </summary>
internal sealed class PowerSupplyEmulatorProtocol(IPowerSupply device) : IDeviceProtocol
{
  private decimal _voltage;
  private decimal _current;
  private bool _output;

  /// <inheritdoc />
  public SemaphoreSlim OperationLock { get; set; } = new(1, 1);

  /// <inheritdoc />
  public async Task<string> QueryAsync(string command, double responseDelay = 0, int timeout = 0,
    int port = 0, int delayBeforeCall = 0, CancellationToken cancellationToken = default)
  {
    await OperationLock.WaitAsync(cancellationToken);
    try
    {
      if (IdleHardwareErrorSimulator.ShouldSimulateHardwareError(device))
        throw new DeviceException(IdleHardwareErrorSimulator.ErrorMessage);
      string[] parts = command.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
      string key = parts[0].ToUpperInvariant();
      if (parts.Length == 2 && key is "VOLT" or "CURR")
      {
        decimal value = decimal.Parse(parts[1], CultureInfo.InvariantCulture);
        if (key == "VOLT") _voltage = value;
        else _current = value;
        return string.Empty;
      }
      if (key == "OUTP" && parts.Length == 2)
      {
        _output = parts[1].Equals("ON", StringComparison.OrdinalIgnoreCase);
        return string.Empty;
      }
      if (key == "*RST") { _voltage = 0; _current = 0; _output = false; return string.Empty; }
      return key switch
      {
        "*IDN?" => "KIP,B5-108,IDLE,1.0",
        "VOLT?" => _voltage.ToString(CultureInfo.InvariantCulture),
        "CURR?" => _current.ToString(CultureInfo.InvariantCulture),
        "VOLT:LIM?" => device.VoltageManager.MaxVoltage.ToString(CultureInfo.InvariantCulture),
        "CURR:LIM?" => device.CurrentManager.MaxCurrent.ToString(CultureInfo.InvariantCulture),
        "OUTP?" => _output ? "1" : "0",
        "MEAS:VOLT?" => (_output ? _voltage : 0).ToString(CultureInfo.InvariantCulture),
        "MEAS:CURR?" => "0",
        "SYST:ERR?" => "0",
        _ => throw new DeviceException($"Эмулятор источника питания не поддерживает команду: {command}"),
      };
    }
    finally { OperationLock.Release(); }
  }
}
