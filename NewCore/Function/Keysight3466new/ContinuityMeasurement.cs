using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter.Capabilities;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using NewCore.Device;
using System.Globalization;

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
    public async Task<bool> SetContinuityModeAsync(IUserInteractionService? userMessageService = null)
    {
      if (await ExecutionConfig.GetIsIdleModeEnabled())
      {
        return true;
      }

      await _device.DeviceProtocol.QueryAsync("CONF:CONT");
      var answer = await _device.DeviceProtocol.QueryAsync("FUNC?", timeout: 1000);
      return answer.Contains("CONT");
    }

    /// <summary>
    /// Проверяет проводимость между измерительными щупами.
    /// </summary>
    /// <returns>
    /// <c>true</c>, если обнаружено соединение (низкое сопротивление), иначе <c>false</c>.
    /// </returns>
    /// <exception cref="InvalidOperationException">Выбрасывается, если прибор не подключен.</exception>
    public async Task<bool> CheckContinuityAsync(bool expectedOutcome, IUserInteractionService? userMessageService = null)
    {
      if (!_device.IsConnected)
      {
        throw new InvalidOperationException("Прибор не подключен.");
      }

      string response = await _device.DeviceProtocol.QueryAsync("MEAS:CONT?", timeout: 1000);
      return response != "+9.90000000E+37" == expectedOutcome;
    }

    /// <summary>
    /// Проверяет проводимость между измерительными щупами.
    /// </summary>
    /// <returns>
    /// <c>true</c>, если обнаружено соединение (низкое сопротивление), иначе <c>false</c>.
    /// </returns>
    /// <exception cref="InvalidOperationException">Выбрасывается, если прибор не подключен.</exception>
    public async Task<double> CheckContinuityAsync(double expectedOutcome, IUserInteractionService? userMessageService = null)
    {
      if (!_device.IsConnected)
      {
        throw new InvalidOperationException("Прибор не подключен.");
      }

      string response = await _device.DeviceProtocol.QueryAsync("MEAS:CONT?", timeout: 1000);
      if (response.Contains("+9.90000000E+37"))
        return 1001;

      if (double.TryParse(response, NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
      {
        return value;
      }

      throw new FormatException($"Неверный формат ответа прибора: '{response}'");
    }
  }
}
