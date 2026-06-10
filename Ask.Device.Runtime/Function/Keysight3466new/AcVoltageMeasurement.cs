using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Extensions;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter.Capabilities;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Device.Runtime.Device;
using Ask.Device.Runtime.Function.Helpers;

namespace Ask.Device.Runtime.Function.Keysight3466new
{
  /// <summary>
  /// Класс для измерения переменного напряжения (AC Voltage) с использованием прибора Keysight.
  /// </summary>
  public class AcVoltageMeasurement : IAcVoltageMeasurement
  {
    private readonly KeysightDevice _device;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="AcVoltageMeasurement"/>.
    /// </summary>
    /// <param name="device">Экземпляр устройства Keysight.</param>
    /// <exception cref="ArgumentNullException">Выбрасывается, если переданный прибор равен <c>null</c>.</exception>
    public AcVoltageMeasurement(KeysightDevice device)
    {
      _device = device ?? throw new ArgumentNullException(nameof(device));
    }

    /// <inheritdoc />
    public async Task<bool> SetACVoltageModeAsync(IUserInteractionService? userMessageService = null)
    {
      if (ExecutionConfig.GetIsIdleModeEnabled())
      {
        return true;
      }
      if (_device.TypeMode == MultimeterTypeMode.AcVoltage)
      {
        return true;
      }

      if (!_device.IsConnected)
      {
        throw new InvalidOperationException("Прибор не подключен.");
      }

      await _device.DeviceProtocol.QueryAsync("CONF:VOLT:AC");
      var answer = await _device.DeviceProtocol.QueryAsync("FUNC?", timeout: 1000);
      if (answer.Contains("VOLT:AC"))
      {
        _device.TypeMode = MultimeterTypeMode.AcVoltage;
        return true;
      }

      return false;
    }

    /// <inheritdoc />
    public async Task<double> MeasureACVoltageAsync(double param = 0, double rangeFrom = -1, double rangeTo = -1, IUserInteractionService? userMessageService = null)
    {
      if (ExecutionConfig.GetIsIdleModeEnabled())
      {
        return MeasurementAdapterHelper.Round(param);
      }

      if (!_device.IsConnected)
      {
        throw new InvalidOperationException("Прибор не подключен.");
      }

      string response = await _device.DeviceProtocol.QueryAsync("MEAS:VOLT:AC?", timeout: 1000);
      response = response.Trim().Replace("+", "");

      if (double.TryParse(response, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double voltage))
      {
        return MeasurementAdapterHelper.Round(voltage);
      }

      throw new FormatException($"Неверный формат ответа прибора при измерении AC-напряжения: '{response}'.");
    }

    /// <inheritdoc />
    public async Task<bool> SetVoltageRangeAsync(VoltageRange mode, IUserInteractionService? userMessageService = null)
    {
      if (mode == VoltageRange.V_1000)
      {
        return false;
      }

      if (ExecutionConfig.GetIsIdleModeEnabled())
      {
        return true;
      }

      if (!_device.IsConnected)
      {
        throw new InvalidOperationException("Прибор не подключен.");
      }

      if (_device.TypeMode != MultimeterTypeMode.AcVoltage)
      {
        throw new InvalidOperationException("Прибор не установлен в режим измерения переменного напряжения.");
      }

      await _device.DeviceProtocol.QueryAsync($"SENS:VOLT:AC:RANGE {mode.GetDisplayName()}");
      var answer = await _device.DeviceProtocol.QueryAsync(mode == VoltageRange.Auto ? "SENS:VOLT:AC:RANGE:AUTO?" : "SENS:VOLT:AC:RANGE?", timeout: 1000);
      if (answer.Contains(mode.GetDisplayDescription()))
      {
        return true;
      }

      return false;
    }
  }
}