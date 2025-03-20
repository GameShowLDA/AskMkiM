using NewCore.Base.Function.FastMeter;
using NewCore.Device;

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
    /// Менеджер связи с прибором.
    /// </summary>
    private readonly ICommunication _communication;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="CapacitanceMeasurement"/>.
    /// </summary>
    /// <param name="device">Экземпляр устройства Keysight.</param>
    /// <exception cref="ArgumentNullException">Выбрасывается, если переданный прибор равен <c>null</c>.</exception>
    public CapacitanceMeasurement(KeysightDevice device)
    {
      _device = device ?? throw new ArgumentNullException(nameof(device));
      _communication = device.CommunicationManager;
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

      await _communication.SendCommandAsync("CONF:CAP");
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
