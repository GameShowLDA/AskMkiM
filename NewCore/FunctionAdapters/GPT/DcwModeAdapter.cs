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
  /// Адаптер режима DCW для GPT-79904 с отображением сообщений.
  /// </summary>
  internal class DcwModeAdapter : IDcwModeBreakdown
  {
    private readonly GPT79904 _device;
    private readonly DcwMode _dcwMode;

    public DcwModeAdapter(GPT79904 device)
    {
      _device = device ?? throw new ArgumentNullException(nameof(device));
      _dcwMode = new DcwMode(device);
    }

    public async Task SetModeAsync()
    {
      await _dcwMode.SetModeAsync();
      await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Установка режима DCW", "DCW", true, 1);
    }

    /// <inheritdoc />
    public async Task<(bool, string)> SetVoltageAsync(double value)
    {
      var result = await _dcwMode.SetVoltageAsync(value);

      await DeviceMessageBuilder.ShowConnectionMessageAsync(
          _device,
          "Установка напряжения DCW",
          result.Item1 ? $"{value} В" : $"Ошибка: {result.Item2}",
          result.Item1,
          1);

      return result;
    }

    public async Task SetHighCurrentLimitAsync(double value)
    {
      await _dcwMode.SetHighCurrentLimitAsync(value);
      await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Установка верхнего предела тока DCW", $"{value} мА", true, 1);
    }

    public async Task SetLowCurrentLimitAsync(double value)
    {
      await _dcwMode.SetLowCurrentLimitAsync(value);
      await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Установка нижнего предела тока DCW", $"{value} мА", true, 1);
    }

    public async Task SetTestTimeAsync(double value)
    {
      await _dcwMode.SetTestTimeAsync(value);
      await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Установка времени теста DCW", $"{value} сек", true, 1);
    }

    public async Task SetRampTimeAsync(double value)
    {
      await _dcwMode.SetRampTimeAsync(value);
      await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Установка Ramp Time DCW", $"{value} сек", true, 1);
    }

    public async Task SetOffsetAsync(double value)
    {
      await _dcwMode.SetOffsetAsync(value);
      await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Установка смещения DCW", $"{value} мА", true, 1);
    }

    public async Task SetArcCurrentAsync(double value)
    {
      await _dcwMode.SetArcCurrentAsync(value);
      await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Установка дугового тока DCW", $"{value} мА", true, 1);
    }

    public async Task<DcwConfiguration> ReadConfigurationAsync()
    {
      var config = await _dcwMode.ReadConfigurationAsync();
      await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Чтение конфигурации DCW", "Конфигурация считана", true, 1);
      return config;
    }

    public async Task<double> MeasureCurrentAsync()
    {
      try
      {
        double result = await _dcwMode.MeasureCurrentAsync();
        await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Измерение тока DCW", $"{result} мА", result >= 0, 2);
        return result;
      }
      catch (Exception ex)
      {
        await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Ошибка измерения тока DCW", ex.Message, false, 2);
        return -1;
      }
    }

    public Task<double> GetRampTimeAsync() => _dcwMode.GetRampTimeAsync();
    public Task<double> GetTestTimeAsync() => _dcwMode.GetTestTimeAsync();

    /// <inheritdoc />
    public async Task<double?> GetVoltageAsync()
    {
      try
      {
        double? result = await _dcwMode.GetVoltageAsync();

        if (result.HasValue)
        {
          await DeviceMessageBuilder.ShowConnectionMessageAsync(
            _device,
            "Чтение напряжения DCW",
            $"{result.Value:F3} кВ",
            true,
            2
          );
          return result;
        }
        else
        {
          await DeviceMessageBuilder.ShowConnectionMessageAsync(
            _device,
            "Чтение напряжения DCW",
            "Не удалось получить значение напряжения",
            false,
            2
          );
          return null;
        }
      }
      catch (Exception ex)
      {
        await DeviceMessageBuilder.ShowConnectionMessageAsync(
          _device,
          "Ошибка при чтении напряжения DCW",
          ex.Message,
          false,
          2
        );
        return null;
      }
    }

  }
}
