using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Errors.Device.Multimeter;
using Ask.Core.Services.UI;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter.Capabilities;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using NewCore.Device;
using NewCore.Function.Helpers;
using NewCore.Function.Keysight3466new;

namespace NewCore.FunctionAdapters.Keysight3466new
{
  /// <summary>
  /// Адаптер измерения ёмкости с использованием прибора Keysight с выводом сообщений.
  /// </summary>
  internal class CapacitanceMeasurementAdapter : ICapacitanceMeasurement
  {
    private readonly KeysightDevice _device;
    private readonly CapacitanceMeasurement _measurement;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="CapacitanceMeasurementAdapter"/>.
    /// </summary>
    /// <param name="device">Экземпляр устройства Keysight.</param>
    public CapacitanceMeasurementAdapter(KeysightDevice device)
    {
      _device = device ?? throw new ArgumentNullException(nameof(device));
      _measurement = new CapacitanceMeasurement(device);
    }

    /// <inheritdoc />
    public async Task<bool> SetCapacitanceModeAsync(IUserInteractionService? userMessageService = null)
    {
      var result = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        var succes = await _measurement.SetCapacitanceModeAsync();

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
      if (rangeTo == -1)
      {
        rangeTo = double.MaxValue;
      }

      var random = Simulated.GetSimulatedValue(rangeFrom, rangeTo, ElectricalTestFunction.Capacitance);
      if (random != -1)
      {
        return random;
      }

      try
      {
        double result = await _measurement.MeasureCapacitanceAsync(param, rangeFrom, rangeTo);
        return result;
      }
      catch (Exception ex)
      {
        await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Ошибка при измерении ёмкости", ex.Message, false, 2, userMessageService);
        return -1;
      }
    }
  }
}
