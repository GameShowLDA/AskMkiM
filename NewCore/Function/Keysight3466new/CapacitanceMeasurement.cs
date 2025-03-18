using NewCore.Base.Function.FastMeter;
using NewCore.Device;

namespace NewCore.Function.Keysight3466new
{
  public class CapacitanceMeasurement : ICapacitanceMeasurement
  {
    private readonly KeysightDevice _device;
    private readonly ICommunication _communication;

    public CapacitanceMeasurement(KeysightDevice device)
    {
      _device = device ?? throw new ArgumentNullException(nameof(device));
      _communication = device.CommunicationManager;
    }

    /// <summary>
    /// Устанавливает прибор в режим измерения ёмкости.
    /// </summary>
    public async Task SetCapacitanceModeAsync()
    {
      if (!_device.IsConnected)
        throw new InvalidOperationException("Прибор не подключен.");

      await _communication.SendCommandAsync("CONF:CAP");
    }

    /// <summary>
    /// Измеряет ёмкость и возвращает результат в нанофарадах.
    /// </summary>
    public async Task<double> MeasureCapacitanceAsync()
    {
      if (!_device.IsConnected)
        throw new InvalidOperationException("Прибор не подключен.");

      string response = await _communication.QueryAsync("MEAS:CAP?");

      // Убираем возможные пробелы и символы `+` перед числом
      response = response.Trim().Replace("+", "");

      if (double.TryParse(response, System.Globalization.NumberStyles.Float,
                          System.Globalization.CultureInfo.InvariantCulture, out double capacitance))
      {
        // Переводим значение в нанофарады (если прибор работает в Фарадах)
        return capacitance * 1e9;
      }

      throw new InvalidOperationException($"Не удалось обработать значение ёмкости: {response}");
    }

  }
}
