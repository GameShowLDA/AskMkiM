using NewCore.Base.Function.FastMeter;
using NewCore.Device;

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

    /// <summary>
    /// Устанавливает режим измерения переменного напряжения (AC Voltage).
    /// </summary>
    public async Task SetACVoltageModeAsync()
    {
      if (!_device.IsConnected)
      {
        throw new InvalidOperationException("Прибор не подключен.");
      }

      await _device.DeviceProtocol.QueryAsync("CONF:VOLT:AC");
    }

    /// <summary>
    /// Измеряет переменное напряжение и возвращает результат.
    /// Если значение некорректно, возвращает -1.
    /// </summary>
    public async Task<double> MeasureACVoltageAsync()
    {
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
