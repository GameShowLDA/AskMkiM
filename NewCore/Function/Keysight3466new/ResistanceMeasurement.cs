using NewCore.Base.Function.FastMeter;
using NewCore.Device;
using static AppConfiguration.Execution.ExecutionConfig;

namespace NewCore.Function.Keysight3466new
{
  /// <summary>
  /// Класс для измерения сопротивления с помощью прибора Keysight.
  /// </summary>
  public class ResistanceMeasurement : IResistanceMeasurement
  {
    private readonly KeysightDevice _device;

    /// <summary>
    /// Создаёт экземпляр класса <see cref="ResistanceMeasurement"/>.
    /// </summary>
    /// <param name="device">Экземпляр устройства Keysight.</param>
    /// <exception cref="ArgumentNullException">Выбрасывается, если переданный прибор <c>null</c>.</exception>
    public ResistanceMeasurement(KeysightDevice device)
    {
      _device = device ?? throw new ArgumentNullException(nameof(device));
    }

    /// <inheritdoc />
    public async Task SetResistanceModeAsync()
    {
      if (await GetIsIdleModeEnabled())
      {
        return;
      }

      if (!_device.IsConnected)
      {
        throw new InvalidOperationException("Прибор не подключен.");
      }

      await _device.DeviceProtocol.QueryAsync("CONF:RES");
    }

    /// <inheritdoc />
    public async Task<double> MeasureResistanceAsync(double param = 0, double rangeFrom = -1, double rangeTo = -1)
    {
      if (await GetIsIdleModeEnabled())
      {
        return param;
      }

      if (!_device.IsConnected)
      {
        throw new InvalidOperationException("Прибор не подключен.");
      }

      string response = await _device.DeviceProtocol.QueryAsync("MEAS:RES?", timeout: 1000);

      // Убираем возможные пробелы и символы `+` перед числом
      response = response.Trim().Replace("+", "");

      if (double.TryParse(response, System.Globalization.NumberStyles.Float,
                          System.Globalization.CultureInfo.InvariantCulture, out double resistance))
      {
        return resistance;
      }

      return -1;
    }
  }
}
