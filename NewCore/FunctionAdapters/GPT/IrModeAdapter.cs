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

    #region Mode

    public async Task<(bool, string)> SetModeAsync()
    {
      var result = await _irMode.SetModeAsync();
      await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Установка режима IR", result.Success ? "IR" : result.Message, result.Success, 1);
      return result;
    }

    public Task<string> GetModeAsync() => _irMode.GetModeAsync();

    #endregion

    #region Voltage

    public async Task<(bool, string)> SetVoltageAsync(double value)
    {
      var result = await _irMode.SetVoltageAsync(value);
      await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Установка напряжения IR", result.Success ? $"{value} В" : result.Message, result.Success, 1);
      return result;
    }

    public async Task<double> GetVoltageAsync()
    {
      var value = await _irMode.GetVoltageAsync();
      await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Чтение напряжения IR", $"{value} В", value > 0, 1);
      return value;
    }

    #endregion

    #region HighResistanceLimit

    public async Task<(bool, string)> SetHighResistanceLimitAsync(double value)
    {
      var result = await _irMode.SetHighResistanceLimitAsync(value);
      await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Установка верхнего предела сопротивления IR", result.Success ? $"{value} ГОм" : result.Message, result.Success, 1);
      return result;
    }

    public Task<double> GetHighResistanceLimitAsync() => _irMode.GetHighResistanceLimitAsync();

    #endregion

    #region LowResistanceLimit

    public async Task<(bool, string)> SetLowResistanceLimitAsync(double value)
    {
      var result = await _irMode.SetLowResistanceLimitAsync(value);
      await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Установка нижнего предела сопротивления IR", result.Success ? $"{value} МОм" : result.Message, result.Success, 1);
      return result;
    }

    public Task<double> GetLowResistanceLimitAsync() => _irMode.GetLowResistanceLimitAsync();

    #endregion

    #region TestTime

    public async Task<(bool, string)> SetTestTimeAsync(double value)
    {
      var result = await _irMode.SetTestTimeAsync(value);
      await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Установка времени измерения IR", result.Success ? $"{value} сек" : result.Message, result.Success, 1);
      return result;
    }

    public Task<double> GetTestTimeAsync() => _irMode.GetTestTimeAsync();

    #endregion

    #region Offset

    public async Task<(bool, string)> SetOffsetAsync(double value)
    {
      var result = await _irMode.SetOffsetAsync(value);
      await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Установка смещения IR", result.Success ? $"{value} ГОм" : result.Message, result.Success, 1);
      return result;
    }

    public Task<double> GetOffsetAsync() => _irMode.GetOffsetAsync();

    #endregion

    #region Измерение и конфигурация

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

    public async Task<IrConfiguration> ReadConfigurationAsync()
    {
      var config = await _irMode.ReadConfigurationAsync();
      await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Чтение конфигурации IR", "Конфигурация считана", true, 1);
      return config;
    }

    public List<int> GetVoltagesForResistance(double resistance) => _irMode.GetVoltagesForResistance(resistance);

    #endregion
  }
}
