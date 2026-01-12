using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter.Capabilities;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using NewCore.Device;
using NewCore.Function.Helpers;
using NewCore.Function.Keysight3466new;

namespace NewCore.FunctionAdapters.Keysight3466new
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
      try
      {
        var result = await _measurement.SetACVoltageModeAsync();

        if (!result || await DeviceDisplayConfig.GetConnectionInfoVisibilityAsync())
        {
          await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Установка режима измерения переменного напряжения", "CONF:VOLT:AC", result, 1, userMessageService);
        }

        return result;
      }
      catch (Exception ex)
      {
        await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Ошибка при установке режима AC", ex.Message, false, 1, userMessageService);
        throw;
      }
    }

    /// <inheritdoc />
    public async Task<double> MeasureACVoltageAsync(double param = 0, IUserInteractionService? userMessageService = null)
    {
      try
      {
        double result = await _measurement.MeasureACVoltageAsync(param);

        await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Результат измерения переменного напряжения", $"{result} В", result >= 0, 1, userMessageService);

        return result;
      }
      catch (Exception ex)
      {
        await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Ошибка при измерении AC-напряжения", ex.Message, false, 1, userMessageService);
        return -1;
      }
    }
  }
}
