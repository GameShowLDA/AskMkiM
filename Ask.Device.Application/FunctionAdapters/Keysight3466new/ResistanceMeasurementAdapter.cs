using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Errors.Device.Multimeter;
using Ask.Core.Services.UI;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter.Capabilities;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Device.Application.Execution;
using NewCore.Device;
using NewCore.Function.Helpers;
using NewCore.Function.Keysight3466new;

namespace NewCore.FunctionAdapters.Keysight3466new
{
  internal class ResistanceMeasurementAdapter : IResistanceMeasurement
  {
    private readonly KeysightDevice _device;
    private readonly ResistanceMeasurement _resistanceMeasurement;

    /// <summary>
    /// Создаёт экземпляр класса <see cref="ResistanceMeasurement"/>.
    /// </summary>
    /// <param name="device">Экземпляр устройства Keysight.</param>
    /// <exception cref="ArgumentNullException">Выбрасывается, если переданный прибор <c>null</c>.</exception>
    public ResistanceMeasurementAdapter(KeysightDevice device)
    {
      _device = device ?? throw new ArgumentNullException(nameof(device));
      _resistanceMeasurement = new ResistanceMeasurement(device);
    }
    /// <inheritdoc />
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
        () => _resistanceMeasurement.MeasureResistanceAsync(param, rangeFrom, rangeTo));

      if (!execution.Success)
      {
        await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Ошибка при измерении сопротивления", execution.ErrorMessage, false, 2, userMessageService);
        return -1;
      }

      double resistance = execution.Value;
      await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Результат измерения сопротивления", $"{resistance} Ом", true, 2, userMessageService);
      return resistance;
    }

    /// <inheritdoc />
    public async Task<bool> SetResistanceModeAsync(IUserInteractionService? userMessageService = null)
    {
      var result = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        var succes = await _resistanceMeasurement.SetResistanceModeAsync();

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
  }
}
