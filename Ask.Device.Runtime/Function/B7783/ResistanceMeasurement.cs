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

      if (!_device.IsConnected)
      {
        throw new InvalidOperationException("Прибор не подключен.");
      }

      await _device.DeviceProtocol.QueryAsync("CONFIGURE:RESISTANCE AUTO", timeout: CommandTimeoutMs);
      string function = await _device.DeviceProtocol.QueryAsync("FUNCTION?", timeout: CommandTimeoutMs);

      if (function.Contains("RES", StringComparison.OrdinalIgnoreCase))
      {
        _device.TypeMode = MultimeterTypeMode.Resistance;
        return true;
      }

      return false;
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

      if (!_device.IsConnected)
      {
        throw new InvalidOperationException("Прибор не подключен.");
      }

      if (_device.TypeMode != MultimeterTypeMode.Resistance)
      {
        bool modeSet = await SetResistanceModeAsync(userMessageService);
        if (!modeSet)
        {
          throw new InvalidOperationException("Не удалось установить режим измерения сопротивления.");
        }
      }

      string response = await _device.DeviceProtocol.QueryAsync("READ?", timeout: MeasurementTimeoutMs);
      response = response.Trim().Replace("+", string.Empty, StringComparison.Ordinal);

      if (double.TryParse(response, NumberStyles.Float, CultureInfo.InvariantCulture, out double resistance))
      {
        return MeasurementAdapterHelper.Round(resistance);
      }

      throw new FormatException($"Неверный формат ответа прибора при измерении сопротивления: '{response}'.");
    }
  }
}
