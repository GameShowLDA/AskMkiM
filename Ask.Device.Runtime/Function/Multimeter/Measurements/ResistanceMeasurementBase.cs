using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Errors.Device.Multimeter;
using Ask.Core.Services.UI;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter.Capabilities;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Device.Runtime.Function.Helpers;

namespace Ask.Device.Runtime.Function.Multimeter.Measurements
{
  internal class ResistanceMeasurementBase : IResistanceMeasurement
  {

    private readonly IFastMeter _device;

    public ResistanceMeasurementBase(IFastMeter device)
    {
      _device = device ?? throw new ArgumentNullException(nameof(device));
    }

    public async Task<double> MeasureResistanceAsync(double param = 0, double rangeFrom = -1, double rangeTo = -1, IUserInteractionService? userMessageService = null)
    {
      if (rangeTo == -1)
      {
        rangeTo = double.MaxValue;
      }

      var random = Simulated.GetSimulatedValue(rangeFrom, rangeTo, ElectricalTestFunction.Resistance);
      if (random != -1)
      {
        return random;
      }

      var execution = await AdapterMeasurementExecutor.ExecuteAsync(
        _device,
        "Измерение сопротивления",
        () => MeasureResistanceCoreAsync(param, rangeFrom, rangeTo));

      if (!execution.Success)
      {
        await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Ошибка при измерении сопротивления", execution.ErrorMessage, false, 2, userMessageService);
        return -1;
      }

      double resistance = execution.Value;
      await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Результат измерения сопротивления", $"{resistance} Ом", true, 2, userMessageService);
      return resistance;
    }

    public async Task<bool> SetResistanceModeAsync(IUserInteractionService? userMessageService = null)
    {
      var result = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        var succes = await SetResistanceModeCoreAsync();

        if (!succes || DeviceDisplayConfig.GetConnectionInfoVisibility())
        {
          await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Установка режима измерения сопротивления", succes, 1);
        }

        return succes;
      }, userMessageService, deviceTask: true);

      if (!result)
      {
        throw ResistanceExceptionFactory.SetModeFailed(_device.Name, _device.NumberChassis, _device.Number);
      }

      _device.TypeMode = MultimeterTypeMode.Resistance;
      return result;
    }

    private async Task<bool> SetResistanceModeCoreAsync()
    {
      if (ExecutionConfig.GetIsIdleModeEnabled())
      {
        return true;
      }
      if (_device.TypeMode == MultimeterTypeMode.Resistance)
      {
        return true;
      }

      if (!_device.IsConnected)
      {
        throw new InvalidOperationException("Прибор не подключен.");
      }

      await _device.DeviceProtocol.QueryAsync(_device.ResistanceCommands.SetMode);
      var answer = await _device.DeviceProtocol.QueryAsync(_device.ResistanceCommands.GetMode, timeout: _device.ResistanceCommands.Timeout);

      if (answer.Contains("RES"))
      {
        _device.TypeMode = MultimeterTypeMode.Resistance;
        return true;
      }

      return false;
    }

    private async Task<double> MeasureResistanceCoreAsync(double param = 0, double rangeFrom = -1, double rangeTo = -1, IUserInteractionService? userMessageService = null)
    {
      if (!_device.IsConnected)
      {
        throw new InvalidOperationException("Прибор не подключен.");
      }

      string response = await _device.DeviceProtocol.QueryAsync(_device.ResistanceCommands.Measure, timeout: _device.ResistanceCommands.Timeout);
      response = response.Trim().Replace("+", "");

      if (double.TryParse(response, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double resistance))
      {
        return MeasurementAdapterHelper.Round(resistance);
      }

      throw new FormatException($"Неверный формат ответа прибора при измерении сопротивления: '{response}'.");
    }
  }
}
