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
  /// Адаптер режима IR (сопротивление изоляции) для GPT-79904 с сообщениями.
  /// </summary>
  internal class IrModeAdapter : IIrModeBreakdown
  {
    private readonly GPT79904 _device;
    private readonly IrMode _irMode;

    public IrModeAdapter(GPT79904 device)
    {
      _device = device ?? throw new ArgumentNullException(nameof(device));
      _irMode = new IrMode(device);
    }

    public async Task SetModeAsync()
    {
      await _irMode.SetModeAsync();
      await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Установка режима IR", "IR", true, 1);
    }

    public async Task SetVoltageAsync(double value)
    {
      await _irMode.SetVoltageAsync(value);
      await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Установка напряжения IR", $"{value} В", true, 1);
    }

    public async Task SetTestTimeAsync(double value)
    {
      await _irMode.SetTestTimeAsync(value);
      await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Установка времени измерения IR", $"{value} сек", true, 1);
    }

    public async Task<double> GetVoltageAsync()
    {
      var value = await _irMode.GetVoltageAsync();
      await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Чтение напряжения IR", $"{value} В", value > 0, 1);
      return value;
    }

    public async Task<double> MeasureResistanceAsync(double param = 0, double rangeFrom = -1, double rangeTo = 60000)
    {
      try
      {
        double result = await _irMode.MeasureResistanceAsync(param, rangeFrom, rangeTo);
        await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Измерение сопротивления IR", $"{result} МОм", result >= rangeFrom && result <= rangeTo, 2);
        return result;
      }
      catch (Exception ex)
      {
        await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Ошибка измерения сопротивления IR", ex.Message, false, 2);
        return -1;
      }
    }

    public List<int> GetVoltagesForResistance(double resistance)
    {
      return _irMode.GetVoltagesForResistance(resistance);
    }

    public async Task SetHighResistanceLimitAsync(double value)
    {
      await _irMode.SetHighResistanceLimitAsync(value);
      await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Установка верхнего предела сопротивления IR", $"{value} ГОм", true, 1);
    }

    public async Task SetLowResistanceLimitAsync(double value)
    {
      await _irMode.SetLowResistanceLimitAsync(value);
      await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Установка нижнего предела сопротивления IR", $"{value} МОм", true, 1);
    }

    public async Task SetOffsetAsync(double value)
    {
      await _irMode.SetOffsetAsync(value);
      await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Установка смещения IR", $"{value} ГОм", true, 1);
    }

    public async Task<IrConfiguration> ReadConfigurationAsync()
    {
      var config = await _irMode.ReadConfigurationAsync();
      await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Чтение конфигурации IR", "Конфигурация считана", true, 1);
      return config;
    }
  }
}
