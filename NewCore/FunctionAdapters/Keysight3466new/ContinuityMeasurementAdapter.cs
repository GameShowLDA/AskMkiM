using System;
using System.Threading.Tasks;
using NewCore.Base.Function.FastMeter;
using NewCore.Device;
using NewCore.Function.Helpers;
using NewCore.Function.Keysight3466new;

namespace NewCore.FunctionAdapters.Keysight3466new
{
  /// <summary>
  /// Адаптер для выполнения прозвонки с использованием прибора Keysight с выводом сообщений.
  /// </summary>
  internal class ContinuityMeasurementAdapter : IContinuityMeasurement
  {
    private readonly KeysightDevice _device;
    private readonly ContinuityMeasurement _measurement;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="ContinuityMeasurementAdapter"/>.
    /// </summary>
    /// <param name="device">Экземпляр прибора Keysight.</param>
    public ContinuityMeasurementAdapter(KeysightDevice device)
    {
      _device = device ?? throw new ArgumentNullException(nameof(device));
      _measurement = new ContinuityMeasurement(device);
    }

    /// <inheritdoc />
    public async Task SetContinuityModeAsync()
    {
      try
      {
        await _measurement.SetContinuityModeAsync();

        await DeviceMessageBuilder.ShowConnectionMessageAsync(
          _device,
          "Установка режима прозвонки",
          string.Empty,
          true,
          1);
      }
      catch (Exception ex)
      {
        await DeviceMessageBuilder.ShowConnectionMessageAsync(
          _device,
          "Ошибка при установке режима прозвонки",
          ex.Message,
          false,
          1);
        throw;
      }
    }

    /// <inheritdoc />
    public async Task<bool> CheckContinuityAsync()
    {
      if (await AppConfiguration.Execution.ExecutionConfig.GetIsIdleModeEnabled())
      {
        return true;
      }

      try
      {
        var result = await _measurement.CheckContinuityAsync();

        await DeviceMessageBuilder.ShowConnectionMessageAsync(
          _device,
          "Результат прозвонки",
          string.Empty,
          true,
          2);

        return result;
      }
      catch (Exception ex)
      {
        await DeviceMessageBuilder.ShowConnectionMessageAsync(
          _device,
          "Ошибка при прозвонке",
          ex.Message,
          false,
          2);
        return false;
      }
    }
  }
}
