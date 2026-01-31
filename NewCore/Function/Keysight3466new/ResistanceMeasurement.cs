using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter.Capabilities;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using NewCore.Device;

namespace NewCore.Function.Keysight3466new
{
  /// <summary>
  /// Класс для измерения сопротивления с помощью прибора Keysight.
  /// </summary>
  public class ResistanceMeasurement : IResistanceMeasurement
  {
    private readonly KeysightDevice _device;

    /// <summary>
    /// Создаёт экземпляр класса <see cref="ResistanceMeasurement"/>.
    /// </summary>
    /// <param name="device">Экземпляр устройства Keysight.</param>
    /// <exception cref="ArgumentNullException">Выбрасывается, если переданный прибор <c>null</c>.</exception>
    public ResistanceMeasurement(KeysightDevice device)
    {
      _device = device ?? throw new ArgumentNullException(nameof(device));
    }

    /// <inheritdoc />
    public async Task<bool> SetResistanceModeAsync(IUserInteractionService? userMessageService = null)
    {
      if (ExecutionConfig.GetIsIdleModeEnabled())
      {
        return true;
      }

      if (!_device.IsConnected)
      {
        throw new InvalidOperationException("Прибор не подключен.");
      }

      await _device.DeviceProtocol.QueryAsync("CONF:RES");
      var answer = await _device.DeviceProtocol.QueryAsync("FUNC?", timeout: 1000);

      return answer.Contains("RES");
    }

    /// <inheritdoc />
    public async Task<double> MeasureResistanceAsync(double param = 0, double rangeFrom = -1, double rangeTo = -1, IUserInteractionService? userMessageService = null)
    {
      if (ExecutionConfig.GetIsIdleModeEnabled())
      {
        return param;
      }

      if (!_device.IsConnected)
      {
        throw new InvalidOperationException("Прибор не подключен.");
      }

      string response = await _device.DeviceProtocol.QueryAsync("MEAS:RES?", timeout: 1000);
      response = response.Trim().Replace("+", "");

      if (double.TryParse(response, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double resistance))
      {
        return resistance;
      }

      return -1;
    }
  }
}