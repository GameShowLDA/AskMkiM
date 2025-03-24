using NewCore.Base.Function.FastMeter;
using NewCore.Device;
using static Utilities.LoggerUtility;

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

    /// <summary>
    /// Устанавливает прибор в режим измерения ёмкости.
    /// </summary>
    /// <exception cref="InvalidOperationException">Выбрасывается, если прибор не подключен.</exception>
    public async Task SetCapacitanceModeAsync()
    {
      if (!_device.IsConnected)
      {
        throw new InvalidOperationException("Прибор не подключен.");
      }

      await _device.DeviceProtocol.QueryAsync("CONF:CAP");
    }

    /// <summary>
    /// Выполняет измерение ёмкости и возвращает результат в нанофарадах (нФ).
    /// </summary>
    /// <returns>Значение ёмкости в нФ.</returns>
    /// <exception cref="InvalidOperationException">
    /// Выбрасывается, если прибор не подключен или не удалось обработать полученное значение.
    /// </exception>
    public async Task<double> MeasureCapacitanceAsync()
    {
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

      throw new InvalidOperationException(LogError($"Не удалось обработать значение ёмкости: {response}"));
    }
  }
}
