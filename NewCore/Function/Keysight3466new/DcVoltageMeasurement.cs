using NewCore.Base.Function.FastMeter;
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
    /// Менеджер связи с прибором.
    /// </summary>
    private readonly ICommunication _communication;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="DcVoltageMeasurement"/>.
    /// </summary>
    /// <param name="device">Экземпляр устройства Keysight.</param>
    /// <exception cref="ArgumentNullException">Выбрасывается, если переданный прибор равен <c>null</c>.</exception>
    public DcVoltageMeasurement(KeysightDevice device)
    {
      _device = device ?? throw new ArgumentNullException(nameof(device));
      _communication = device.CommunicationManager;
    }

    /// <summary>
    /// Устанавливает прибор в режим измерения постоянного напряжения (DC Voltage).
    /// </summary>
    /// <exception cref="InvalidOperationException">Выбрасывается, если прибор не подключен.</exception>
    public async Task SetDCVoltageModeAsync()
    {
      if (!_device.IsConnected)
      {
        throw new InvalidOperationException("Прибор не подключен.");
      }

      await _communication.SendCommandAsync("CONF:VOLT:DC");
    }

    /// <summary>
    /// Измеряет постоянное напряжение и возвращает его значение.
    /// </summary>
    /// <returns>
    /// Значение измеренного напряжения в вольтах. Если измерение некорректное, возвращает -1.
    /// </returns>
    /// <exception cref="InvalidOperationException">Выбрасывается, если прибор не подключен.</exception>
    public async Task<double> MeasureDCVoltageAsync()
    {
      if (!_device.IsConnected)
      {
        throw new InvalidOperationException("Прибор не подключен.");
      }

      string response = await _communication.QueryAsync("MEAS:VOLT:DC?");
      response = response.Trim().Replace("+", "");

      if (double.TryParse(response, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double voltage))
      {
        return voltage;
      }

      return -1;
    }
  }
}
