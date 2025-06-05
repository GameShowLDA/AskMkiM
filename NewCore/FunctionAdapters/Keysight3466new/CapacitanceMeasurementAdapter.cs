using System;
using System.Threading.Tasks;
using NewCore.Base.Function.FastMeter;
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
    public async Task SetCapacitanceModeAsync()
    {
      try
      {
        await _measurement.SetCapacitanceModeAsync();

        await DeviceMessageBuilder.ShowConnectionMessageAsync(
          _device,
          "Установка режима измерения ёмкости",
          true,
          1);
      }
      catch (Exception ex)
      {
        await DeviceMessageBuilder.ShowConnectionMessageAsync(
          _device,
          "Ошибка установки режима ёмкости",
          ex.Message,
          false,
          1);
        throw;
      }
    }

    /// <inheritdoc />
    public async Task<double> MeasureCapacitanceAsync(double param = 0)
    {
      try
      {
        double result = await _measurement.MeasureCapacitanceAsync(param);

        await DeviceMessageBuilder.ShowConnectionMessageAsync(
          _device,
          "Результат измерения ёмкости",
          $"{result} нФ",
          result >= 0,
          2);

        return result;
      }
      catch (Exception ex)
      {
        await DeviceMessageBuilder.ShowConnectionMessageAsync(
          _device,
          "Ошибка при измерении ёмкости",
          ex.Message,
          false,
          2);
        return -1;
      }
    }
  }
}
