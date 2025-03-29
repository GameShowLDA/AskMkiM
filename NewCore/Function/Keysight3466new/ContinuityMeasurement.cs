using NewCore.Base.Function.FastMeter;
using NewCore.Device;
using static AppConfiguration.Execution.ExecutionConfig;

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
    /// Инициализирует новый экземпляр класса <see cref="ContinuityMeasurement"/>.
    /// </summary>
    /// <param name="device">Экземпляр устройства Keysight.</param>
    /// <exception cref="ArgumentNullException">Выбрасывается, если переданный прибор равен <c>null</c>.</exception>
    public ContinuityMeasurement(KeysightDevice device)
    {
      _device = device ?? throw new ArgumentNullException(nameof(device));
    }

    /// <summary>
    /// Устанавливает прибор в режим прозвонки (Continuity Test).
    /// </summary>
    /// <exception cref="InvalidOperationException">Выбрасывается, если прибор не подключен.</exception>
    public async Task SetContinuityModeAsync()
    {
      if (await GetIsIdleModeEnabled())
      {
        return;
      }

      await _device.DeviceProtocol.QueryAsync("CONF:CONT");
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

      string response = await _device.DeviceProtocol.QueryAsync("MEAS:CONT?", timeout: 1000);
      return response != "+9.90000000E+37";
    }
  }
}
