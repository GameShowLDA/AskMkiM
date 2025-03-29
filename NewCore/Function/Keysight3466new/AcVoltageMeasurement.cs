using NewCore.Base.Function.FastMeter;
using NewCore.Device;
using static AppConfiguration.Execution.ExecutionConfig;

namespace NewCore.Function.Keysight3466new
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
    public async Task SetACVoltageModeAsync()
    {
      if (await GetIsIdleModeEnabled())
      {
        return;
      }

      if (!_device.IsConnected)
      {
        throw new InvalidOperationException("Прибор не подключен.");
      }

      await _device.DeviceProtocol.QueryAsync("CONF:VOLT:AC");
    }

    /// <inheritdoc />
    public async Task<double> MeasureACVoltageAsync(double param = 0)
    {
      if (await GetIsIdleModeEnabled())
      {
        return param;
      }

      if (!_device.IsConnected)
      {
        throw new InvalidOperationException("Прибор не подключен.");
      }

      string response = await _device.DeviceProtocol.QueryAsync("CONF:VOLT:AC", timeout: 1000);

      response = response.Trim().Replace("+", "");

      if (double.TryParse(response, System.Globalization.NumberStyles.Float,
                          System.Globalization.CultureInfo.InvariantCulture, out double voltage))
      {
        return voltage;
      }

      return -1;
    }
  }
}
