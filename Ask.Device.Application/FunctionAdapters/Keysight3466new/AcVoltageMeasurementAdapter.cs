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
  /// Адаптер для измерения переменного напряжения с использованием прибора Keysight с выводом сообщений.
  /// </summary>
  internal class AcVoltageMeasurementAdapter : IAcVoltageMeasurement
  {
    private readonly KeysightDevice _device;
    private readonly AcVoltageMeasurement _measurement;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="AcVoltageMeasurementAdapter"/>.
    /// </summary>
    /// <param name="device">Экземпляр устройства Keysight.</param>
    public AcVoltageMeasurementAdapter(KeysightDevice device)
    {
      _device = device ?? throw new ArgumentNullException(nameof(device));
      _measurement = new AcVoltageMeasurement(device);
    }

    /// <inheritdoc />
    public async Task<bool> SetACVoltageModeAsync(IUserInteractionService? userMessageService = null)
    {
      var result = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        var succes = await _measurement.SetACVoltageModeAsync();
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
        () => _measurement.MeasureACVoltageAsync(param, rangeFrom, rangeTo));

      if (!execution.Success)
      {
        await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Ошибка при измерении AC-напряжения", execution.ErrorMessage, false, 1, userMessageService);
        return -1;
      }

      double result = execution.Value;
      await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Результат измерения переменного напряжения", $"{result} В", true, 1, userMessageService);

      return result;
    }

    public async Task<bool> SetVoltageRangeAsync(VoltageRange mode, IUserInteractionService? userMessageService = null)
    {
      return true;
    }
  }
}