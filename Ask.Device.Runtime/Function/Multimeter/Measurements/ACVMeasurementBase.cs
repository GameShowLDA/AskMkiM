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
  internal class ACVMeasurementBase : IAcVoltageMeasurement
  {
    private readonly IFastMeter _device;
    public ACVMeasurementBase(IFastMeter device)
    {
      _device = device ?? throw new ArgumentNullException(nameof(device));
    }

    /// <inheritdoc />
    public async Task<bool> SetACVoltageModeAsync(IUserInteractionService? userMessageService = null)
    {
      var result = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        var succes = await SetACVoltageModeCoreAsync();
        if (!succes || DeviceDisplayConfig.GetConnectionInfoVisibility())
        {
          await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Установка режима измерения переменного напряжения", succes, 1, userMessageService);
        }

        return succes;
      }, userMessageService, deviceTask: true);

      if (!result)
      {
        throw AcExceptionFactory.SetModeFailed(_device.Name, _device.NumberChassis, _device.Number);
      }

      return result;
    }

    /// <inheritdoc />
    public async Task<double> MeasureACVoltageAsync(double param = 0, double rangeFrom = -1, double rangeTo = -1, IUserInteractionService? userMessageService = null)
    {
      if (_device.TypeMode != MultimeterTypeMode.AcVoltage)
      {
        await SetACVoltageModeAsync(userMessageService);
      }

      if (rangeTo == -1)
      {
        rangeTo = double.MaxValue;
      }

      var random = Simulated.GetSimulatedValue(rangeFrom, rangeTo, ElectricalTestFunction.ACVoltage);
      if (random != -1)
      {
        return random;
      }

      var execution = await AdapterMeasurementExecutor.ExecuteAsync(
        _device,
        "Измерение переменного напряжения",
        () => MeasureACVoltageCoreAsync(param, rangeFrom, rangeTo));

      if (!execution.Success)
      {
        await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Ошибка при измерении AC-напряжения", execution.ErrorMessage, false, 1, userMessageService);
        return -1;
      }

      double result = execution.Value;
      await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Результат измерения переменного напряжения", $"{result} В", true, 1, userMessageService);

      return result;
    }

    /// <inheritdoc />
    private async Task<bool> SetACVoltageModeCoreAsync(IUserInteractionService? userMessageService = null)
    {
      if (ExecutionConfig.GetIsIdleModeEnabled())
      {
        return true;
      }
      if (_device.TypeMode == MultimeterTypeMode.AcVoltage)
      {
        return true;
      }

      if (!_device.IsConnected)
      {
        throw new InvalidOperationException("Прибор не подключен.");
      }

      await _device.DeviceProtocol.QueryAsync(_device.ACVCommands.SetMode);
      var answer = await _device.DeviceProtocol.QueryAsync(_device.ACVCommands.GetMode, timeout: _device.ACVCommands.Timeout);
      if (answer.Contains(_device.ACVCommands.CheckMode))
      {
        _device.TypeMode = MultimeterTypeMode.AcVoltage;
        return true;
      }

      return false;
    }

    /// <inheritdoc />
    private async Task<double> MeasureACVoltageCoreAsync(double param = 0, double rangeFrom = -1, double rangeTo = -1, IUserInteractionService? userMessageService = null)
    {
      if (ExecutionConfig.GetIsIdleModeEnabled())
      {
        return MeasurementAdapterHelper.Round(param);
      }

      if (!_device.IsConnected)
      {
        throw new InvalidOperationException("Прибор не подключен.");
      }

      string response = await _device.DeviceProtocol.QueryAsync(_device.ACVCommands.Measure, timeout: _device.ACVCommands.Timeout);
      response = response.Trim().Replace("+", "");

      if (double.TryParse(response, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double voltage))
      {
        return MeasurementAdapterHelper.Round(voltage);
      }

      throw new FormatException($"Неверный формат ответа прибора при измерении AC-напряжения: '{response}'.");
    }

  }
}
