using NewCore.Base.Function.FastMeter;
using NewCore.Device;

namespace NewCore.Function.Keysight3466new
{
  public class DcVoltageMeasurement : IDcVoltageMeasurement
  {
    private readonly KeysightDevice _device;
    private readonly ICommunication _communication;

    public DcVoltageMeasurement(KeysightDevice device)
    {
      _device = device ?? throw new ArgumentNullException(nameof(device));
      _communication = device.CommunicationManager;
    }


    /// <summary>
    /// Устанавливает режим измерения постоянного напряжения (DC Voltage).
    /// </summary>
    public async Task SetDCVoltageModeAsync()
    {
      if (!_device.IsConnected)
        throw new InvalidOperationException("Прибор не подключен.");

      await _communication.SendCommandAsync("CONF:VOLT:DC");
    }

    /// <summary>
    /// Измеряет постоянное напряжение и возвращает результат.
    /// Если значение некорректно, возвращает -1.
    /// </summary>
    public async Task<double> MeasureDCVoltageAsync()
    {
      if (!_device.IsConnected)
        throw new InvalidOperationException("Прибор не подключен.");

      string response = await _communication.QueryAsync("MEAS:VOLT:DC?");
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
