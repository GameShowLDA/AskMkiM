using NewCore.Base.Function.FastMeter;
using NewCore.Device;

namespace NewCore.Function.Keysight3466new
{
  /// <summary>
  /// Класс для измерения сопротивления с помощью прибора Keysight.
  /// </summary>
  public class ResistanceMeasurement : IResistanceMeasurement
  {
    private readonly KeysightDevice _device;
    private readonly ICommunication _communication;

    /// <summary>
    /// Создаёт экземпляр класса <see cref="ResistanceMeasurement"/>.
    /// </summary>
    /// <param name="device">Экземпляр устройства Keysight.</param>
    /// <exception cref="ArgumentNullException">Выбрасывается, если переданный прибор <c>null</c>.</exception>
    public ResistanceMeasurement(KeysightDevice device)
    {
      _device = device ?? throw new ArgumentNullException(nameof(device));
      _communication = device.CommunicationManager;
    }

    /// <summary>
    /// Устанавливает прибор в режим измерения сопротивления.
    /// </summary>
    public async Task SetResistanceModeAsync()
    {
      if (!_device.IsConnected)
      {
        throw new InvalidOperationException("Прибор не подключен.");
      }

      await _communication.SendCommandAsync("CONF:RES");
    }

    /// <summary>
    /// Измеряет сопротивление и возвращает результат.
    /// Если значение некорректно, возвращает -1.
    /// </summary>
    public async Task<double> MeasureResistanceAsync()
    {
      if (!_device.IsConnected)
      {
        throw new InvalidOperationException("Прибор не подключен.");
      }

      string response = await _communication.QueryAsync("MEAS:RES?");

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
