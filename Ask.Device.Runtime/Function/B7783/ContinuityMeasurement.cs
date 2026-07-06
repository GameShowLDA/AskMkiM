using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter.Capabilities;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Device.Runtime.Device;
using Ask.Device.Runtime.Function.Helpers;
using System.Globalization;

namespace Ask.Device.Runtime.Function.B7783
{
  public sealed class ContinuityMeasurement : IContinuityMeasurement
  {
    private const int CommandTimeoutMs = 5000;
    private const int MeasurementTimeoutMs = 10000;
    private const string OpenCircuitMarker = "9.90000000E+37";

    private readonly MultimeterB7783 _device;

    public ContinuityMeasurement(MultimeterB7783 device)
    {
      _device = device ?? throw new ArgumentNullException(nameof(device));
    }

    public async Task<bool> SetContinuityModeAsync(IUserInteractionService? userMessageService = null)
    {
      if (ExecutionConfig.GetIsIdleModeEnabled())
      {
        return true;
      }

      if (_device.TypeMode == MultimeterTypeMode.Continuity)
      {
        return true;
      }

      EnsureConnected();

      _device.TypeMode = MultimeterTypeMode.None;
      await _device.DeviceProtocol.QueryAsync("*CLS", timeout: CommandTimeoutMs);
      await TryAbortAsync();

      if (await TrySetContinuityModeAsync("CONF:CONT AUTO"))
      {
        return true;
      }

      if (await TrySetContinuityModeAsync("CONF:CONT"))
      {
        return true;
      }

      if (await TrySetContinuityModeAsync("FUNC \"CONT\"", "CONF:CONT"))
      {
        return true;
      }

      if (await TrySetContinuityModeAsync("SENSE:FUNCTION \"CONT\"", "CONF:CONT"))
      {
        return true;
      }

      if (await TrySetContinuityModeAsync("CONFIGURE:CONTINUITY AUTO"))
      {
        return true;
      }

      string function = await ReadFunctionAsync();
      string error = await ReadInstrumentErrorAsync();
      throw new InvalidOperationException($"Failed to set B7-78/3 continuity mode. FUNCTION?={function}; SYSTEM:ERROR?={error}");
    }

    public async Task<bool> CheckContinuityAsync(bool expectedOutcome, IUserInteractionService? userMessageService = null)
    {
      if (ExecutionConfig.GetIsIdleModeEnabled())
      {
        return true;
      }

      EnsureConnected();

      if (_device.TypeMode != MultimeterTypeMode.Continuity)
      {
        bool modeSet = await SetContinuityModeAsync(userMessageService);
        if (!modeSet)
        {
          throw new InvalidOperationException("Failed to set B7-78/3 continuity measurement mode.");
        }
      }

      double resistance = await ReadContinuityResistanceAsync();
      bool actualOutcome = resistance <= _device.MaxContinuityResistance;
      return actualOutcome == expectedOutcome;
    }

    public async Task<double> CheckContinuityAsync(
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

      if (_device.TypeMode != MultimeterTypeMode.Continuity)
      {
        bool modeSet = await SetContinuityModeAsync(userMessageService);
        if (!modeSet)
        {
          throw new InvalidOperationException("Failed to set B7-78/3 continuity measurement mode.");
        }
      }

      double resistance = await ReadContinuityResistanceAsync();
      return MeasurementAdapterHelper.Round(resistance);
    }

    private async Task<bool> TrySetContinuityModeAsync(params string[] commands)
    {
      foreach (string command in commands)
      {
        await _device.DeviceProtocol.QueryAsync(command, timeout: CommandTimeoutMs);
      }

      string function = await ReadFunctionAsync();
      if (function.Contains("CONT", StringComparison.OrdinalIgnoreCase))
      {
        _device.TypeMode = MultimeterTypeMode.Continuity;
        return true;
      }

      _device.TypeMode = MultimeterTypeMode.None;
      return false;
    }

    private async Task<double> ReadContinuityResistanceAsync()
    {
      string response = await _device.DeviceProtocol.QueryAsync("READ?", timeout: MeasurementTimeoutMs);
      response = response.Trim().Replace("+", string.Empty, StringComparison.Ordinal);

      if (response.Contains(OpenCircuitMarker, StringComparison.OrdinalIgnoreCase))
      {
        return 1001d;
      }

      if (double.TryParse(response, NumberStyles.Float, CultureInfo.InvariantCulture, out double resistance))
      {
        return resistance;
      }

      throw new FormatException($"Invalid B7-78/3 continuity response: '{response}'.");
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
  }
}
