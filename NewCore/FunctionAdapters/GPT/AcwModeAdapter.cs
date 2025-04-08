using System;
using System.Threading.Tasks;
using NewCore.Base.Function.Breakdown;
using NewCore.Device;
using NewCore.Function.GPT;
using NewCore.Function.GPT.Data;
using NewCore.Function.Helpers;

namespace NewCore.FunctionAdapters.GPT
{
  /// <summary>
  /// Адаптер режима ACW для устройства GPT-79904 с отображением сообщений.
  /// </summary>
  internal class AcwModeAdapter : IAcwModeBreakdown
  {
    private readonly GPT79904 _device;
    private readonly AcwMode _acwMode;

    public AcwModeAdapter(GPT79904 device)
    {
      _device = device ?? throw new ArgumentNullException(nameof(device));
      _acwMode = new AcwMode(device);
    }

    /// <inheritdoc />
    public async Task SetModeAsync()
    {
      await _acwMode.SetModeAsync();
      await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Установка режима ACW", "ACW", true, 1);
    }

    /// <inheritdoc />
    public async Task SetVoltageAsync(double value)
    {
      await _acwMode.SetVoltageAsync(value);
      await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Установка напряжения ACW", $"{value} В", true, 1);
    }

    /// <inheritdoc />
    public async Task SetHighCurrentLimitAsync(double value)
    {
      await _acwMode.SetHighCurrentLimitAsync(value);
      await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Установка верхнего предела тока ACW", $"{value} мА", true, 1);
    }

    /// <inheritdoc />
    public async Task SetLowCurrentLimitAsync(double value)
    {
      await _acwMode.SetLowCurrentLimitAsync(value);
      await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Установка нижнего предела тока ACW", $"{value} мА", true, 1);
    }

    /// <inheritdoc />
    public async Task SetTestTimeAsync(double value)
    {
      await _acwMode.SetTestTimeAsync(value);
      await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Установка времени теста ACW", $"{value} сек", true, 1);
    }

    /// <inheritdoc />
    public async Task SetRampTimeAsync(double value)
    {
      await _acwMode.SetRampTimeAsync(value);
      await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Установка Ramp Time ACW", $"{value} сек", true, 1);
    }

    /// <inheritdoc />
    public async Task SetFrequencyAsync(int frequency)
    {
      try
      {
        await _acwMode.SetFrequencyAsync(frequency);
        await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Установка частоты ACW", $"{frequency} Гц", true, 1);
      }
      catch (Exception ex)
      {
        await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Ошибка установки частоты ACW", ex.Message, false, 1);
        throw;
      }
    }

    /// <inheritdoc />
    public async Task SetOffsetAsync(double value)
    {
      await _acwMode.SetOffsetAsync(value);
      await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Установка смещения ACW", $"{value} мА", true, 1);
    }

    /// <inheritdoc />
    public async Task SetArcCurrentAsync(double value)
    {
      await _acwMode.SetArcCurrentAsync(value);
      await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Установка дугового тока ACW", $"{value} мА", true, 1);
    }

    /// <inheritdoc />
    public async Task<AcwConfiguration> ReadConfigurationAsync()
    {
      var config = await _acwMode.ReadConfigurationAsync();
      await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Чтение конфигурации ACW", "Конфигурация считана", true, 1);
      return config;
    }

    /// <inheritdoc />
    public async Task<double> MeasureCurrentAsync(double param = 0)
    {
      try
      {
        double result = await _acwMode.MeasureCurrentAsync(param);
        await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Измерение тока ACW", $"{result} мА", result >= 0, 2);
        return result;
      }
      catch (Exception ex)
      {
        await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Ошибка измерения тока ACW", ex.Message, false, 2);
        return -1;
      }
    }

    /// <inheritdoc />
    public Task<double> GetRampTimeAsync() => _acwMode.GetRampTimeAsync();

    /// <inheritdoc />
    public Task<double> GetTestTimeAsync() => _acwMode.GetTestTimeAsync();
  }
}
