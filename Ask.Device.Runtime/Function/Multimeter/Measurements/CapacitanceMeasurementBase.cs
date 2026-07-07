using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Errors.Device.Multimeter;
using Ask.Core.Services.UI;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter.Capabilities;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Device.Runtime.Function.Helpers;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Device.Runtime.Function.Multimeter.Measurements
{
  internal class CapacitanceMeasurementBase : ICapacitanceMeasurement
  {
    private readonly IFastMeter _device;

    public CapacitanceMeasurementBase(IFastMeter device)
    {
      _device = device ?? throw new ArgumentNullException(nameof(device));
    }

    /// <inheritdoc />
    public async Task<bool> SetCapacitanceModeAsync(IUserInteractionService? userMessageService = null)
    {
      var result = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        var succes = await SetCapacitanceModeCoreAsync();

        if (!succes || DeviceDisplayConfig.GetConnectionInfoVisibility())
        {
          await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Установка режима измерения ёмкости", succes, 1, userMessageService);
        }

        return succes;
      }, userMessageService, deviceTask: true);

      if (!result)
      {
        throw CapacitanceExceptionFactory.SetModeFailed(_device.Name, _device.NumberChassis, _device.Number);
      }

      return result;
    }

    /// <inheritdoc />
    public async Task<double> MeasureCapacitanceAsync(double param = 0, double rangeFrom = -1, double rangeTo = -1, IUserInteractionService? userMessageService = null)
    {
      if (_device.TypeMode != MultimeterTypeMode.Capacitance)
      {
        await SetCapacitanceModeAsync(userMessageService);
      }

      if (rangeTo == -1)
      {
        rangeTo = double.MaxValue;
      }

      var random = Simulated.GetSimulatedValue(rangeFrom, rangeTo, ElectricalTestFunction.Capacitance);
      if (random != -1)
      {
        return random;
      }

      var execution = await AdapterMeasurementExecutor.ExecuteAsync(
        _device,
        "Измерение ёмкости",
        () => MeasureCapacitanceCoreAsync(param, rangeFrom, rangeTo));

      if (!execution.Success)
      {
        await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Ошибка при измерении ёмкости", execution.ErrorMessage, false, 2, userMessageService);
        return -1;
      }

      double result = execution.Value;
      await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Результат измерения ёмкости", $"{result} нФ", true, 2, userMessageService);
      return result;
    }

    /// <inheritdoc />
    public async Task<bool> SetCapacitanceModeCoreAsync(IUserInteractionService? userMessageService = null)
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

      await _device.DeviceProtocol.QueryAsync(_device.CapacitanceCommands.SetMode);
      var answer = await _device.DeviceProtocol.QueryAsync(_device.CapacitanceCommands.GetMode, timeout: _device.CapacitanceCommands.Timeout);
      if (answer.Contains(_device.CapacitanceCommands.CheckMode))
      {
        _device.TypeMode = MultimeterTypeMode.Capacitance;
        return true;
      }

      return false;
    }

    /// <inheritdoc />
    public async Task<double> MeasureCapacitanceCoreAsync(double param = 0, double rangeFrom = -1, double rangeTo = -1, IUserInteractionService? userMessageService = null)
    {
      if (ExecutionConfig.GetIsIdleModeEnabled())
      {
        return MeasurementAdapterHelper.Round(param);
      }

      if (!_device.IsConnected)
      {
        throw new InvalidOperationException("Прибор не подключен.");
      }

      string response = await _device.DeviceProtocol.QueryAsync(_device.CapacitanceCommands.Measure, responseDelay: 1500, timeout: _device.CapacitanceCommands.Timeout);
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
