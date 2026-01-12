using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter.Capabilities;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using NewCore.Device;
using NewCore.Function.Helpers;
using NewCore.Function.Keysight3466new;

namespace NewCore.FunctionAdapters.Keysight3466new
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
      try
      {
        var result = await _measurement.SetDCVoltageModeAsync();

        if (!result || await DeviceDisplayConfig.GetConnectionInfoVisibilityAsync())
        {
          await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Установка режима измерения постоянного напряжения", "CONF:VOLT:DC", result, 1, userMessageService);
        }

        return result;
      }
      catch (Exception ex)
      {
        await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Ошибка при установке режима DC", ex.Message, false, 1, userMessageService);
        throw;
      }
    }

    /// <inheritdoc />
    public async Task<double> MeasureDCVoltageAsync(double param = 0, IUserInteractionService? userMessageService = null)
    {
      try
      {
        double result = await _measurement.MeasureDCVoltageAsync(param);

        await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Результат измерения постоянного напряжения", $"{result}В", result >= 0, 2, userMessageService);

        return result;
      }
      catch (Exception ex)
      {
        await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Ошибка при измерении DC-напряжения", ex.Message, false, 2, userMessageService);
        return -1;
      }
    }
  }
}
