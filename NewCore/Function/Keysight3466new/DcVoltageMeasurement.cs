using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter.Capabilities;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using NewCore.Device;

namespace NewCore.Function.Keysight3466new
{
  /// <summary>
  /// Класс для измерения постоянного напряжения (DC Voltage) с использованием прибора Keysight.
  /// </summary>
  public class DcVoltageMeasurement : IDcVoltageMeasurement
  {
    /// <summary>
    /// Экземпляр прибора Keysight.
    /// </summary>
    private readonly KeysightDevice _device;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="DcVoltageMeasurement"/>.
    /// </summary>
    /// <param name="device">Экземпляр устройства Keysight.</param>
    /// <exception cref="ArgumentNullException">Выбрасывается, если переданный прибор равен <c>null</c>.</exception>
    public DcVoltageMeasurement(KeysightDevice device)
    {
      _device = device ?? throw new ArgumentNullException(nameof(device));
    }

    /// <inheritdoc />
    public async Task<bool> SetDCVoltageModeAsync(IUserInteractionService? userMessageService = null)
    {
      if (ExecutionConfig.GetIsIdleModeEnabled())
      {
        return true;
      }
      if (_device.TypeMode == MultimeterTypeMode.DcVoltage)
      {
        return true;
      }

      if (!_device.IsConnected)
      {
        throw new InvalidOperationException("Прибор не подключен.");
      }

      await _device.DeviceProtocol.QueryAsync("CONF:VOLT:DC");
      var answer = await _device.DeviceProtocol.QueryAsync("FUNC?", timeout: 1000);
      if (answer.Contains("VOLT"))
      {
        _device.TypeMode = MultimeterTypeMode.DcVoltage;
        return true;
      }

      return false;
    }

    /// <inheritdoc />
    public async Task<double> MeasureDCVoltageAsync(double param = 0, IUserInteractionService? userMessageService = null)
    {
      if (ExecutionConfig.GetIsIdleModeEnabled())
      {
        return param;
      }

      if (!_device.IsConnected)
      {
        throw new InvalidOperationException("Прибор не подключен.");
      }

      string response = await _device.DeviceProtocol.QueryAsync("MEAS:VOLT:DC?", timeout: 1000);
      response = response.Trim().Replace("+", "");

      if (double.TryParse(response, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double voltage))
      {
        return voltage;
      }

      return -1;
    }
  }
}
