using NewCore.Base.Function.FastMeter;
using NewCore.Device;
using static Utilities.LoggerUtility;
using static AppConfiguration.Execution.ExecutionConfig;

namespace NewCore.Function.Keysight3466new
{
  /// <summary>
  /// Класс для измерения ёмкости с использованием прибора Keysight.
  /// </summary>
  public class CapacitanceMeasurement : ICapacitanceMeasurement
  {
    /// <summary>
    /// Экземпляр прибора Keysight.
    /// </summary>
    private readonly KeysightDevice _device;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="CapacitanceMeasurement"/>.
    /// </summary>
    /// <param name="device">Экземпляр устройства Keysight.</param>
    /// <exception cref="ArgumentNullException">Выбрасывается, если переданный прибор равен <c>null</c>.</exception>
    public CapacitanceMeasurement(KeysightDevice device)
    {
      _device = device ?? throw new ArgumentNullException(nameof(device));
    }

    /// <inheritdoc />
    public async Task SetCapacitanceModeAsync()
    {
      if (await GetIsIdleModeEnabled())
      {
        return;
      }

      if (!_device.IsConnected)
      {
        throw new InvalidOperationException("Прибор не подключен.");
      }

      await _device.DeviceProtocol.QueryAsync("CONF:CAP");
    }

    /// <inheritdoc />
    public async Task<double> MeasureCapacitanceAsync(double param = 0)
    {
      if (await GetIsIdleModeEnabled())
      {
        return param;
      }

      if (!_device.IsConnected)
      {
        throw new InvalidOperationException("Прибор не подключен.");
      }

      string response = await _device.DeviceProtocol.QueryAsync("MEAS:CAP?", timeout: 1000);
      response = response.Trim().Replace("+", "");

      if (double.TryParse(response, System.Globalization.NumberStyles.Float,
                          System.Globalization.CultureInfo.InvariantCulture, out double capacitance))
      {
        // Переводим значение в нанофарады (если прибор работает в Фарадах)
        return capacitance * 1e9;
      }

      throw new InvalidOperationException(LogError($"Не удалось обработать значение ёмкости: {response}", isDeviceLog: true));
    }
  }
}
