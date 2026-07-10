using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter.Capabilities;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Device.Runtime.Device;
using Ask.Device.Runtime.Function.Helpers;
using System.Globalization;

namespace Ask.Device.Runtime.Function.B7783
{
  public sealed class ResistanceMeasurement : IResistanceMeasurement
  {
    private const int CommandTimeoutMs = 5000;
    private const int MeasurementTimeoutMs = 10000;
    private readonly MultimeterB7783 _device;

    public ResistanceMeasurement(MultimeterB7783 device)
    {
      _device = device ?? throw new ArgumentNullException(nameof(device));
    }

    public async Task<bool> SetResistanceModeAsync(IUserInteractionService? userMessageService = null)
    {
      if (ExecutionConfig.GetIsIdleModeEnabled())
      {
        return true;
      }

      if (_device.TypeMode == MultimeterTypeMode.Resistance)
      {
        return true;
      }

      EnsureConnected();

      _device.TypeMode = MultimeterTypeMode.None;
      await _device.DeviceProtocol.QueryAsync("*CLS", timeout: CommandTimeoutMs);
      await TryAbortAsync();

      if (await TrySetResistanceModeAsync("FUNC \"RES\"", "CONF:RES"))
      {
        return true;
      }

      if (await TrySetResistanceModeAsync("SENSE:FUNCTION \"RES\"", "CONF:RES"))
      {
        return true;
      }

      if (await TrySetResistanceModeAsync("CONF:RES"))
      {
        return true;
      }

      if (await TrySetResistanceModeAsync("CONFIGURE:RESISTANCE AUTO"))
      {
        return true;
      }

      await _device.DeviceProtocol.QueryAsync("*RST", timeout: CommandTimeoutMs);
      await Task.Delay(500);
      await _device.DeviceProtocol.QueryAsync("*CLS", timeout: CommandTimeoutMs);
      if (await TrySetResistanceModeAsync("FUNC \"RES\"", "CONF:RES"))
      {
        return true;
      }

      string function = await _device.DeviceProtocol.QueryAsync("FUNCTION?", timeout: CommandTimeoutMs);
      string error = await ReadInstrumentErrorAsync();
      throw new InvalidOperationException($"Failed to set B7-78/3 resistance mode. FUNCTION?={function}; SYSTEM:ERROR?={error}");
    }

    public async Task<double> MeasureResistanceAsync(
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

      if (_device.TypeMode != MultimeterTypeMode.Resistance)
      {
        bool modeSet = await SetResistanceModeAsync(userMessageService);
        if (!modeSet)
        {
          throw new InvalidOperationException("Failed to set B7-78/3 resistance measurement mode.");
        }
      }

      string response = await _device.DeviceProtocol.QueryAsync("READ?", timeout: MeasurementTimeoutMs);
      response = response.Trim().Replace("+", string.Empty, StringComparison.Ordinal);

      if (double.TryParse(response, NumberStyles.Float, CultureInfo.InvariantCulture, out double resistance))
      {
        return MeasurementAdapterHelper.Round(resistance);
      }

      throw new FormatException($"Invalid B7-78/3 resistance response: '{response}'.");
    }

    private async Task<bool> TrySetResistanceModeAsync(params string[] commands)
    {
      foreach (string command in commands)
      {
        await _device.DeviceProtocol.QueryAsync(command, timeout: CommandTimeoutMs);
      }

      string function = await _device.DeviceProtocol.QueryAsync("FUNCTION?", timeout: CommandTimeoutMs);

      if (function.Contains("RES", StringComparison.OrdinalIgnoreCase))
      {
        _device.TypeMode = MultimeterTypeMode.Resistance;
        return true;
      }

      _device.TypeMode = MultimeterTypeMode.None;
      return false;
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
