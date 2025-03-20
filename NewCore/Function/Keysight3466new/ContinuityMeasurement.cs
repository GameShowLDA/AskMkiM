using NewCore.Base.Function.FastMeter;
using NewCore.Device;

namespace NewCore.Function.Keysight3466new
{
  /// <summary>
  /// Класс для выполнения прозвонки (Continuity Test) с использованием прибора Keysight.
  /// </summary>
  public class ContinuityMeasurement : IContinuityMeasurement
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
    /// Инициализирует новый экземпляр класса <see cref="ContinuityMeasurement"/>.
    /// </summary>
    /// <param name="device">Экземпляр устройства Keysight.</param>
    /// <exception cref="ArgumentNullException">Выбрасывается, если переданный прибор равен <c>null</c>.</exception>
    public ContinuityMeasurement(KeysightDevice device)
    {
      _device = device ?? throw new ArgumentNullException(nameof(device));
      _communication = device.CommunicationManager;
    }

    /// <summary>
    /// Устанавливает прибор в режим прозвонки (Continuity Test).
    /// </summary>
    /// <exception cref="InvalidOperationException">Выбрасывается, если прибор не подключен.</exception>
    public async Task SetContinuityModeAsync()
    {
      if (!_device.IsConnected)
      {
        throw new InvalidOperationException("Прибор не подключен.");
      }

      await _communication.SendCommandAsync("CONF:CONT");
    }

    /// <summary>
    /// Проверяет проводимость между измерительными щупами.
    /// </summary>
    /// <returns>
    /// <c>true</c>, если обнаружено соединение (низкое сопротивление), иначе <c>false</c>.
    /// </returns>
    /// <exception cref="InvalidOperationException">Выбрасывается, если прибор не подключен.</exception>
    public async Task<bool> CheckContinuityAsync()
    {
      if (!_device.IsConnected)
      {
        throw new InvalidOperationException("Прибор не подключен.");
      }

      string response = await _communication.QueryAsync("MEAS:CONT?");

      // Если прибор возвращает +9.90000000E+37, значит цепь разомкнута (нет связи)
      return response != "+9.90000000E+37";
    }
  }
}
