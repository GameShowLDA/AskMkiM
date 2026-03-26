using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter.Capabilities;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Device.Runtime.Device;
using System.Globalization;

namespace Ask.Device.Runtime.Function.Keysight3466new
{
  /// <summary>
  /// Класс для проверки диода с помощью прибора Keysight.
  /// </summary>
  public class DiodeMeasurement : IDiodeMeasurement
  {
    private readonly KeysightDevice _device;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="DiodeMeasurement"/>.
    /// </summary>
    /// <param name="device">Экземпляр устройства Keysight.</param>
    /// <exception cref="ArgumentNullException">Выбрасывается, если переданный прибор равен <c>null</c>.</exception>
    public DiodeMeasurement(KeysightDevice device)
    {
      _device = device ?? throw new ArgumentNullException(nameof(device));
    }

    /// <inheritdoc />
    public async Task<bool> SetDiodeModeAsync(IUserInteractionService? userMessageService = null)
    {
      if (ExecutionConfig.GetIsIdleModeEnabled())
      {
        return true;
      }

      if (_device.TypeMode == MultimeterTypeMode.Diode)
      {
        return true;
      }

      if (!_device.IsConnected)
      {
        throw new InvalidOperationException("Прибор не подключен.");
      }

      await _device.DeviceProtocol.QueryAsync("CONF:DIOD");
      var answer = await _device.DeviceProtocol.QueryAsync("FUNC?", timeout: 1000);
      if (answer.Contains("DIOD"))
      {
        _device.TypeMode = MultimeterTypeMode.Diode;
        return true;
      }

      return false;
    }

    /// <inheritdoc />
    public async Task<double> CheckDiodeAsync(double param = 0, double rangeFrom = -1, double rangeTo = -1, IUserInteractionService? userMessageService = null)
    {
      if (_device.IsConnected == false)
      {
        throw new InvalidOperationException("Прибор не подключен.");
      }

      string response = await _device.DeviceProtocol.QueryAsync("MEAS:DIOD?", timeout: 1000);
      response = response.Trim().Replace("+", "");

      if (double.TryParse(response, NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
      {
        return value;
      }

      throw new FormatException($"Неверный формат ответа прибора: '{response}'");
    }
  }
}
