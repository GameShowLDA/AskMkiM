using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Device.Runtime.Device;
using System.Globalization;

namespace Ask.Device.Runtime.Function.B7783
{
  public abstract class VoltageMeasurementBase
  {
    private const int CommandTimeoutMs = 5000;
    private const int MeasurementTimeoutMs = 15000;

    private readonly double[] _supportedRanges;

    protected VoltageMeasurementBase(MultimeterB7783 device, double[] supportedRanges)
    {
      Device = device ?? throw new ArgumentNullException(nameof(device));
      _supportedRanges = supportedRanges ?? throw new ArgumentNullException(nameof(supportedRanges));
    }

    protected MultimeterB7783 Device { get; }

    protected abstract string FunctionName { get; }

    protected abstract string ScpiFunctionName { get; }

    protected abstract MultimeterTypeMode TargetMode { get; }

    protected abstract string ModeResponseToken { get; }

    protected async Task<bool> SetVoltageModeAsync(
      double param = 0,
      double rangeFrom = -1,
      double rangeTo = -1,
      IUserInteractionService? userMessageService = null)
    {
      if (ExecutionConfig.GetIsIdleModeEnabled())
      {
        return true;
      }

      if (Device.TypeMode == TargetMode && !HasMeasurementConfiguration(param, rangeFrom, rangeTo))
      {
        return true;
      }

      EnsureConnected();

      await ConfigureVoltageMeasurementAsync(param, rangeFrom, rangeTo);
      string function = await Device.DeviceProtocol.QueryAsync("FUNCTION?", timeout: CommandTimeoutMs);

      if (function.Contains(ModeResponseToken, StringComparison.OrdinalIgnoreCase))
      {
        Device.TypeMode = TargetMode;
        return true;
      }

      return false;
    }

    protected async Task<double> MeasureVoltageAsync(
      double param = 0,
      double rangeFrom = -1,
      double rangeTo = -1,
      IUserInteractionService? userMessageService = null)
    {
      if (ExecutionConfig.GetIsIdleModeEnabled())
      {
        return param;
      }

      EnsureConnected();

      if (Device.TypeMode != TargetMode || HasMeasurementConfiguration(param, rangeFrom, rangeTo))
      {
        bool modeSet = await SetVoltageModeAsync(param, rangeFrom, rangeTo, userMessageService);
        if (!modeSet)
        {
          throw new InvalidOperationException($"Failed to set B7-78/3 {FunctionName} voltage measurement mode.");
        }
      }

      string response = await Device.DeviceProtocol.QueryAsync("READ?", timeout: MeasurementTimeoutMs);
      response = response.Trim().Replace("+", string.Empty, StringComparison.Ordinal);

      if (double.TryParse(response, NumberStyles.Float, CultureInfo.InvariantCulture, out double voltage))
      {
        return voltage;
      }

      throw new FormatException($"Invalid B7-78/3 {FunctionName} voltage response: '{response}'.");
    }

    private async Task ConfigureVoltageMeasurementAsync(double param, double rangeFrom, double rangeTo)
    {
      double? range = ResolveRange(param, rangeFrom, rangeTo);
      await Device.DeviceProtocol.QueryAsync("*CLS", timeout: CommandTimeoutMs);

      if (range.HasValue)
      {
        string rangeValue = Format(range.Value);
        string resolution = Format(ResolveResolution(range.Value));
        await Device.DeviceProtocol.QueryAsync($"CONF:{ScpiFunctionName} {rangeValue},{resolution}", timeout: CommandTimeoutMs);
      }
      else
      {
        await Device.DeviceProtocol.QueryAsync($"CONF:{ScpiFunctionName} AUTO", timeout: CommandTimeoutMs);
      }

      await EnsureNoInstrumentErrorAsync();
    }

    private async Task EnsureNoInstrumentErrorAsync()
    {
      string error = await Device.DeviceProtocol.QueryAsync("SYSTEM:ERROR?", timeout: CommandTimeoutMs);
      if (!string.IsNullOrWhiteSpace(error) && !error.StartsWith("+0", StringComparison.Ordinal))
      {
        throw new InvalidOperationException($"B7-78/3 configuration error: {error}");
      }
    }

    private double? ResolveRange(double param, double rangeFrom, double rangeTo)
    {
      double requested = Math.Max(Math.Abs(param), Math.Max(Math.Abs(rangeFrom), Math.Abs(rangeTo)));
      if (requested <= 0 || double.IsInfinity(requested) || double.IsNaN(requested))
      {
        return null;
      }

      foreach (double range in _supportedRanges)
      {
        if (requested <= range)
        {
          return range;
        }
      }

      return _supportedRanges[^1];
    }

    private static bool HasMeasurementConfiguration(double param, double rangeFrom, double rangeTo)
    {
      return param != 0 || rangeFrom >= 0 || rangeTo >= 0;
    }

    private static double ResolveResolution(double range)
    {
      return range switch
      {
        <= 0.1d => 0.0000001d,
        <= 1d => 0.000001d,
        <= 10d => 0.00001d,
        <= 100d => 0.0001d,
        _ => 0.001d
      };
    }

    private void EnsureConnected()
    {
      if (!Device.ConnectionInfo.IsConnected)
      {
        throw new InvalidOperationException("Device is not connected.");
      }
    }

    private static string Format(double value)
    {
      return value.ToString("G", CultureInfo.InvariantCulture);
    }
  }
}
