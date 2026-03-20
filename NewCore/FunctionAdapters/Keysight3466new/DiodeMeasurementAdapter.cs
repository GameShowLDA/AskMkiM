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
  /// Адаптер проверки диода с использованием прибора Keysight с выводом сообщений.
  /// </summary>
  internal class DiodeMeasurementAdapter : IDiodeMeasurement
  {
    private readonly KeysightDevice _device;
    private readonly DiodeMeasurement _measurement;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="DiodeMeasurementAdapter"/>.
    /// </summary>
    /// <param name="device">Экземпляр устройства Keysight.</param>
    public DiodeMeasurementAdapter(KeysightDevice device)
    {
      _device = device ?? throw new ArgumentNullException(nameof(device));
      _measurement = new DiodeMeasurement(device);
    }

    /// <inheritdoc />
    public async Task<bool> SetDiodeModeAsync(IUserInteractionService? userMessageService = null)
    {
      var result = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        var succes = await _measurement.SetDiodeModeAsync();

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
      if (rangeTo == -1)
      {
        rangeTo = double.MaxValue;
      }

      var random = Simulated.GetSimulatedValue(rangeFrom, rangeTo, ElectricalTestFunction.Diode);
      if (random != -1)
      {
        return random;
      }

      try
      {
        double result = await _measurement.CheckDiodeAsync(param, rangeFrom, rangeTo);

        await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Результат проверки диода", $"{result} В", result >= 0, 2, userMessageService);
        return result;
      }
      catch (Exception ex)
      {
        await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Ошибка при проверке диода", ex.Message, false, 2, userMessageService);
        return -1;
      }
    }
  }
}
