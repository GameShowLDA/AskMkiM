using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter.Capabilities;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Device.Runtime.Device;
using Ask.Device.Runtime.Function.Helpers;
using System.Globalization;

namespace Ask.Device.Runtime.Function.B7783
{
  public sealed class CapacitanceMeasurement : ICapacitanceMeasurement
  {
    private const int CommandTimeoutMs = 5000;
    private const int MeasurementTimeoutMs = 20000;
    private const double NanofaradsInFarad = 1e9d;

    private static readonly double[] SupportedRangesNanofarads =
    [
      1d,
      10d,
      100d,
      1000d,
      10000d,
      100000d
    ];

    private readonly MultimeterB7783 _device;

    public CapacitanceMeasurement(MultimeterB7783 device)
    {
      _device = device ?? throw new ArgumentNullException(nameof(device));
    }

    public async Task<bool> SetCapacitanceModeAsync(IUserInteractionService? userMessageService = null)
    {
      if (ExecutionConfig.GetIsIdleModeEnabled())
      {
        return true;
      }

      if (_device.TypeMode == MultimeterTypeMode.Capacitance)
      {
        return true;
      }

      EnsureConnected();

      _device.TypeMode = MultimeterTypeMode.None;
      await _device.DeviceProtocol.QueryAsync("*CLS", timeout: CommandTimeoutMs);
      await TryAbortAsync();

      if (await TrySetCapacitanceModeAsync("CONF:CAP AUTO"))
      {
        return true;
      }

      if (await TrySetCapacitanceModeAsync("FUNC \"CAP\"", "CONF:CAP"))
      {
        return true;
      }

      if (await TrySetCapacitanceModeAsync("SENSE:FUNCTION \"CAP\"", "CONF:CAP"))
      {
        return true;
      }

      if (await TrySetCapacitanceModeAsync("CONF:CAP"))
      {
        return true;
      }

      string function = await ReadFunctionAsync();
      string error = await ReadInstrumentErrorAsync();
      throw new InvalidOperationException($"Failed to set B7-78/3 capacitance mode. FUNCTION?={function}; SYSTEM:ERROR?={error}");
    }

    public async Task<bool> SetCapacitanceRangeAsync(double rangeNanofarads, IUserInteractionService? userMessageService = null)
    {
      if (ExecutionConfig.GetIsIdleModeEnabled())
      {
        return true;
      }

      if (rangeNanofarads <= 0 || double.IsNaN(rangeNanofarads) || double.IsInfinity(rangeNanofarads))
      {
        throw new ArgumentOutOfRangeException(nameof(rangeNanofarads), "Capacitance range must be positive.");
      }

      EnsureConnected();

      double rangeFarads = rangeNanofarads / NanofaradsInFarad;
      string range = Format(rangeFarads);
      string resolution = Format(ResolveResolutionFarads(rangeFarads));

      _device.TypeMode = MultimeterTypeMode.None;
      await _device.DeviceProtocol.QueryAsync("*CLS", timeout: CommandTimeoutMs);
      await _device.DeviceProtocol.QueryAsync($"CONF:CAP {range},{resolution}", timeout: CommandTimeoutMs);
      await EnsureNoInstrumentErrorAsync();

      string function = await ReadFunctionAsync();
      if (function.Contains("CAP", StringComparison.OrdinalIgnoreCase))
      {
        _device.TypeMode = MultimeterTypeMode.Capacitance;
        return true;
      }

      return false;
    }

    public async Task<double> MeasureCapacitanceAsync(
      double param = 0,
      double rangeFrom = -1,
      double rangeTo = -1,
      IUserInteractionService? userMessageService = null)
    {
      if (ExecutionConfig.GetIsIdleModeEnabled())
      {
        return MeasurementAdapterHelper.Round(param);
      }

      EnsureConnected();

      double? range = ResolveRange(param, rangeFrom, rangeTo);
      if (range.HasValue)
      {
        bool rangeSet = await SetCapacitanceRangeAsync(range.Value, userMessageService);
        if (!rangeSet)
        {
          throw new InvalidOperationException("Failed to set B7-78/3 capacitance measurement range.");
        }
      }
      else if (_device.TypeMode != MultimeterTypeMode.Capacitance)
      {
        bool modeSet = await SetCapacitanceModeAsync(userMessageService);
        if (!modeSet)
        {
          throw new InvalidOperationException("Failed to set B7-78/3 capacitance measurement mode.");
        }
      }

      string response = await _device.DeviceProtocol.QueryAsync("READ?", responseDelay: 1500, timeout: MeasurementTimeoutMs);
      double farads = ParseFarads(response);
      return MeasurementAdapterHelper.Round(farads * NanofaradsInFarad);
    }

    private async Task<bool> TrySetCapacitanceModeAsync(params string[] commands)
    {
      foreach (string command in commands)
      {
        await _device.DeviceProtocol.QueryAsync(command, timeout: CommandTimeoutMs);
      }

      string function = await ReadFunctionAsync();
      if (function.Contains("CAP", StringComparison.OrdinalIgnoreCase))
      {
        _device.TypeMode = MultimeterTypeMode.Capacitance;
        return true;
      }

      _device.TypeMode = MultimeterTypeMode.None;
      return false;
    }

    private double? ResolveRange(double param, double rangeFrom, double rangeTo)
    {
      double requested = Math.Max(Math.Abs(param), Math.Max(Math.Abs(rangeFrom), Math.Abs(rangeTo)));
      if (requested <= 0 || double.IsInfinity(requested) || double.IsNaN(requested))
      {
        return null;
      }

      foreach (double range in SupportedRangesNanofarads)
      {
        if (requested <= range)
        {
          return range;
        }
      }

      return SupportedRangesNanofarads[^1];
    }

    private static double ParseFarads(string response)
    {
      string normalized = response.Trim().Replace("+", string.Empty, StringComparison.Ordinal);
      string[] parts = normalized.Split([',', ';', ' '], StringSplitOptions.RemoveEmptyEntries);
      string valueText = parts.Length > 0 ? parts[0] : normalized;

      if (double.TryParse(valueText, NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
      {
        return value;
      }

      throw new FormatException($"Invalid B7-78/3 capacitance response: '{response}'.");
    }

    private async Task TryAbortAsync()
    {
      try
      {
        await _device.DeviceProtocol.QueryAsync("ABORT", timeout: CommandTimeoutMs);
      }
      catch
      {
      }
    }

    private async Task EnsureNoInstrumentErrorAsync()
    {
      string error = await ReadInstrumentErrorAsync();
      if (!string.IsNullOrWhiteSpace(error) && !error.StartsWith("+0", StringComparison.Ordinal))
      {
        throw new InvalidOperationException($"B7-78/3 capacitance configuration error: {error}");
      }
    }

    private async Task<string> ReadFunctionAsync()
    {
      try
      {
        return await _device.DeviceProtocol.QueryAsync("FUNCTION?", timeout: CommandTimeoutMs);
      }
      catch
      {
        return await _device.DeviceProtocol.QueryAsync("FUNC?", timeout: CommandTimeoutMs);
      }
    }

    private async Task<string> ReadInstrumentErrorAsync()
    {
      try
      {
        return await _device.DeviceProtocol.QueryAsync("SYSTEM:ERROR?", timeout: CommandTimeoutMs);
      }
      catch (Exception ex)
      {
        return ex.Message;
      }
    }

    private void EnsureConnected()
    {
      if (!_device.IsConnected)
      {
        throw new InvalidOperationException("Device is not connected.");
      }
    }

    private static double ResolveResolutionFarads(double rangeFarads)
    {
      return rangeFarads switch
      {
        <= 1e-9d => 1e-15d,
        <= 10e-9d => 10e-15d,
        <= 100e-9d => 100e-15d,
        <= 1e-6d => 1e-12d,
        <= 10e-6d => 10e-12d,
        _ => 100e-12d
      };
    }

    private static string Format(double value)
    {
      return value.ToString("G", CultureInfo.InvariantCulture);
    }
  }
}
