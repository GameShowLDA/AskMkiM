using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Errors.Device.Multimeter;
using Ask.Core.Services.UI;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter.Capabilities;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Device.Application.Execution;
using Ask.Device.Application.Function.Helpers;
using Ask.Device.Runtime.Device;
using Ask.Device.Runtime.Function.Helpers;
using Ask.Device.Runtime.Function.Keysight3466new;

namespace Ask.Device.Application.FunctionAdapters.Keysight3466new
{
  /// <summary>
  /// Адаптер измерения постоянного напряжения с использованием прибора Keysight с выводом сообщений.
  /// </summary>
  internal class DcVoltageMeasurementAdapter : IDcVoltageMeasurement
  {
    private readonly KeysightDevice _device;
    private readonly DcVoltageMeasurement _measurement;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="DcVoltageMeasurementAdapter"/>.
    /// </summary>
    /// <param name="device">Экземпляр устройства Keysight.</param>
    public DcVoltageMeasurementAdapter(KeysightDevice device)
    {
      _device = device ?? throw new ArgumentNullException(nameof(device));
      _measurement = new DcVoltageMeasurement(device);
    }

    /// <inheritdoc />
    public async Task<bool> SetDCVoltageModeAsync(IUserInteractionService? userMessageService = null)
    {
      var result = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        var succes = await _measurement.SetDCVoltageModeAsync();

        if (!succes || DeviceDisplayConfig.GetConnectionInfoVisibility())
        {
          await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Установка режима измерения постоянного напряжения", succes, 1, userMessageService);
        }

        return succes;
      }, userMessageService, deviceTask: true);

      if (!result)
      {
        throw DcExceptionFactory.SetModeFailed(_device.Name, _device.NumberChassis, _device.Number);
      }

      return result;
    }

    /// <inheritdoc />
    public async Task<double> MeasureDCVoltageAsync(double param = 0, double rangeFrom = -1, double rangeTo = -1, IUserInteractionService? userMessageService = null)
    {
      if (rangeTo == -1)
      {
        rangeTo = double.MaxValue;
      }

      var random = Simulated.GetSimulatedValue(rangeFrom, rangeTo, ElectricalTestFunction.DCVoltage);
      if (random != -1)
      {
        return random;
      }

      var execution = await AdapterMeasurementExecutor.ExecuteAsync(
        _device,
        "Измерение постоянного напряжения",
        () => _measurement.MeasureDCVoltageAsync(param, rangeFrom, rangeTo));

      if (!execution.Success)
      {
        await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Ошибка при измерении DC-напряжения", execution.ErrorMessage, false, 2, userMessageService);
        return -1;
      }

      double result = execution.Value;
      await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Результат измерения постоянного напряжения", $"{result}В", true, 2, userMessageService);

      return result;
    }

    /// <inheritdoc />
    public async Task<bool> SetVoltageRangeAsync(VoltageRange mode, IUserInteractionService? userMessageService = null)
    {
      return true;
    }
  }
}
