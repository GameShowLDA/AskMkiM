using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter.Capabilities;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Device.Runtime.Device;
using Ask.Device.Runtime.Function.Helpers;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Device.Runtime.Function.Keysight3466new
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

    /// <inheritdoc />
    public async Task<bool> SetCapacitanceModeAsync(IUserInteractionService? userMessageService = null)
    {
      if (ExecutionConfig.GetIsIdleModeEnabled())
      {
        return true;
      }
      if (_device.TypeMode == MultimeterTypeMode.Capacitance)
      {
        return true;
      }

      if (!_device.IsConnected)
      {
        throw new InvalidOperationException("Прибор не подключен.");
      }

      await _device.DeviceProtocol.QueryAsync("CONF:CAP");
      var answer = await _device.DeviceProtocol.QueryAsync("FUNC?", timeout: 1000);
      if (answer.Contains("CAP"))
      {
        _device.TypeMode = MultimeterTypeMode.Capacitance;
        return true;
      }

      return false;
    }

    /// <inheritdoc />
    public async Task<double> MeasureCapacitanceAsync(double param = 0, double rangeFrom = -1, double rangeTo = -1, IUserInteractionService? userMessageService = null)
    {
      if (ExecutionConfig.GetIsIdleModeEnabled())
      {
        return MeasurementAdapterHelper.Round(param);
      }

      if (!_device.IsConnected)
      {
        throw new InvalidOperationException("Прибор не подключен.");
      }

      string response = await _device.DeviceProtocol.QueryAsync("MEAS:CAP?", responseDelay: 1500, timeout: 1000);
      response = response.Trim().Replace("+", "");

      if (double.TryParse(response, System.Globalization.NumberStyles.Float,
                          System.Globalization.CultureInfo.InvariantCulture, out double capacitance))
      {
        return MeasurementAdapterHelper.Round(capacitance * 1e9);
      }

      throw new InvalidOperationException(LogError($"Не удалось обработать значение ёмкости: {response}", isDeviceLog: true));
    }
  }
}
