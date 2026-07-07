using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Errors.Device.Multimeter;
using Ask.Core.Services.UI;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter.Capabilities;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Device.Runtime.Function.Helpers;
using System.Globalization;

namespace Ask.Device.Runtime.Function.Multimeter.Measurements
{
  internal class DiodeMeasurementBase : IDiodeMeasurement
  {
    private readonly IFastMeter _device;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="DiodeMeasurement"/>.
    /// </summary>
    /// <param name="device">Экземпляр устройства Keysight.</param>
    /// <exception cref="ArgumentNullException">Выбрасывается, если переданный прибор равен <c>null</c>.</exception>
    public DiodeMeasurementBase(IFastMeter device)
    {
      _device = device ?? throw new ArgumentNullException(nameof(device));
    }

    /// <inheritdoc />
    public async Task<bool> SetDiodeModeAsync(IUserInteractionService? userMessageService = null)
    {
      var result = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        var succes = await SetDiodeModeCoreAsync();

        if (!succes || DeviceDisplayConfig.GetConnectionInfoVisibility())
        {
          await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Установка режима проверки диода", succes, 1, userMessageService);
        }

        return succes;
      }, userMessageService, deviceTask: true);

      if (!result)
      {
        throw DiodeExceptionFactory.SetModeFailed(_device.Name, _device.NumberChassis, _device.Number);
      }

      _device.TypeMode = MultimeterTypeMode.Diode;
      return result;
    }

    /// <inheritdoc />
    public async Task<double> CheckDiodeAsync(double param = 0, double rangeFrom = -1, double rangeTo = -1, IUserInteractionService? userMessageService = null)
    {
      if (_device.TypeMode != MultimeterTypeMode.Diode)
      {
        await SetDiodeModeCoreAsync(userMessageService);
      }

      if (rangeTo == -1)
      {
        rangeTo = double.MaxValue;
      }

      var random = Simulated.GetSimulatedValue(rangeFrom, rangeTo, ElectricalTestFunction.Diode);
      if (random != -1)
      {
        return random;
      }

      var execution = await AdapterMeasurementExecutor.ExecuteAsync(
        _device,
        "Проверка диода",
        () => CheckDiodeCoreAsync(param, rangeFrom, rangeTo));

      if (!execution.Success)
      {
        await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Ошибка при проверке диода", execution.ErrorMessage, false, 2, userMessageService);
        return -1;
      }

      double result = execution.Value;
      await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Результат проверки диода", $"{result} В", true, 2, userMessageService);
      return result;
    }

    /// <inheritdoc />
    public async Task<bool> SetDiodeModeCoreAsync(IUserInteractionService? userMessageService = null)
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

      await _device.DeviceProtocol.QueryAsync(_device.DiodeCommands.SetMode);
      var answer = await _device.DeviceProtocol.QueryAsync(_device.DiodeCommands.GetMode, timeout: _device.DiodeCommands.Timeout);
      if (answer.Contains(_device.DiodeCommands.CheckMode))
      {
        _device.TypeMode = MultimeterTypeMode.Diode;
        return true;
      }

      return false;
    }

    /// <inheritdoc />
    public async Task<double> CheckDiodeCoreAsync(double param = 0, double rangeFrom = -1, double rangeTo = -1, IUserInteractionService? userMessageService = null)
    {
      if (_device.IsConnected == false)
      {
        throw new InvalidOperationException("Прибор не подключен.");
      }

      string response = await _device.DeviceProtocol.QueryAsync(_device.DiodeCommands.Measure, timeout: _device.DiodeCommands.Timeout);
      response = response.Trim().Replace("+", "");

      if (double.TryParse(response, NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
      {
        return MeasurementAdapterHelper.Round(value);
      }

      throw new FormatException($"Неверный формат ответа прибора: '{response}'");
    }
  }
}
