using System;
using System.Threading.Tasks;
using NewCore.Base.Function.Breakdown;
using NewCore.Device;
using NewCore.Function.GPT;
using NewCore.Function.GPT.Data;
using NewCore.Function.Helpers;
using Utilities.Error.Device.Breakdown;

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

    #region Mode

    /// <inheritdoc />
    public async Task<(bool, string)> SetModeAsync()
    {
      var result = await _acwMode.SetModeAsync();
      await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Установка режима ACW", result.Success ? "ACW" : result.Message, result.Success, 1);

      if (!result.Success)
        throw AcwExceptionFactory.SetModeFailed(_device.Name, _device.NumberChassis, _device.Number, result.Message);

      return result;
    }

    /// <inheritdoc />
    public Task<string> GetModeAsync() => _acwMode.GetModeAsync();

    #endregion

    #region Voltage

    /// <inheritdoc />
    public async Task<(bool, string)> SetVoltageAsync(double value)
    {
      var result = await _acwMode.SetVoltageAsync(value);
      await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Установка напряжения ACW", result.Success ? $"{value} В" : result.Message, result.Success, 1);

      if (!result.Success)
        throw AcwExceptionFactory.SetVoltageFailed(_device.Name, _device.NumberChassis, _device.Number, result.Message);

      return result;
    }

    /// <inheritdoc />
    public Task<double> GetVoltageAsync() => _acwMode.GetVoltageAsync();

    #endregion

    #region HighCurrentLimit

    /// <inheritdoc />
    public async Task<(bool, string)> SetHighCurrentLimitAsync(double value)
    {
      var result = await _acwMode.SetHighCurrentLimitAsync(value);
      await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Установка верхнего предела тока ACW", result.Success ? $"{value} мА" : result.Message, result.Success, 1);

      if (!result.Success)
        throw AcwExceptionFactory.SetHighLimitFailed(_device.Name, _device.NumberChassis, _device.Number, result.Message);

      return result;
    }

    /// <inheritdoc />
    public Task<double> GetHighCurrentLimitAsync() => _acwMode.GetHighCurrentLimitAsync();

    #endregion

    #region LowCurrentLimit

    /// <inheritdoc />
    public async Task<(bool, string)> SetLowCurrentLimitAsync(double value)
    {
      var result = await _acwMode.SetLowCurrentLimitAsync(value);
      await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Установка нижнего предела тока ACW", result.Success ? $"{value} мА" : result.Message, result.Success, 1);

      if (!result.Success)
        throw AcwExceptionFactory.SetLowLimitFailed(_device.Name, _device.NumberChassis, _device.Number, result.Message);

      return result;
    }

    /// <inheritdoc />
    public Task<double> GetLowCurrentLimitAsync() => _acwMode.GetLowCurrentLimitAsync();

    #endregion

    #region TestTime

    /// <inheritdoc />
    public async Task<(bool, string)> SetTestTimeAsync(double value)
    {
      var result = await _acwMode.SetTestTimeAsync(value);
      await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Установка времени теста ACW", result.Success ? $"{value} сек" : result.Message, result.Success, 1);

      if (!result.Success)
        throw AcwExceptionFactory.SetTestTimeFailed(_device.Name, _device.NumberChassis, _device.Number, result.Message);

      return result;
    }

    /// <inheritdoc />
    public Task<double> GetTestTimeAsync() => _acwMode.GetTestTimeAsync();

    #endregion

    #region RampTime

    /// <inheritdoc />
    public async Task<(bool, string)> SetRampTimeAsync(double value)
    {
      var result = await _acwMode.SetRampTimeAsync(value);
      await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Установка Ramp Time ACW", result.Success ? $"{value} сек" : result.Message, result.Success, 1);

      if (!result.Success)
        throw AcwExceptionFactory.SetRampTimeFailed(_device.Name, _device.NumberChassis, _device.Number, result.Message);

      return result;
    }

    /// <inheritdoc />
    public Task<double> GetRampTimeAsync() => _acwMode.GetRampTimeAsync();

    #endregion

    #region Frequency

    /// <inheritdoc />
    public async Task<(bool, string)> SetFrequencyAsync(int frequency)
    {
      var result = await _acwMode.SetFrequencyAsync(frequency);
      await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Установка частоты ACW", result.Success ? $"{frequency} Гц" : result.Message, result.Success, 1);

      if (!result.Success)
        throw AcwExceptionFactory.SetFrequencyFailed(_device.Name, _device.NumberChassis, _device.Number, result.Message);

      return result;
    }

    /// <inheritdoc />
    public Task<int> GetFrequencyAsync() => _acwMode.GetFrequencyAsync();

    #endregion

    #region Offset

    /// <inheritdoc />
    public async Task<(bool, string)> SetOffsetAsync(double value)
    {
      var result = await _acwMode.SetOffsetAsync(value);
      await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Установка смещения ACW", result.Success ? $"{value} мА" : result.Message, result.Success, 1);

      if (!result.Success)
        throw AcwExceptionFactory.SetOffsetFailed(_device.Name, _device.NumberChassis, _device.Number, result.Message);

      return result;
    }


    /// <inheritdoc />
    public Task<double> GetOffsetAsync() => _acwMode.GetOffsetAsync();

    #endregion

    #region ArcCurrent

    /// <inheritdoc />
    public async Task<(bool, string)> SetArcCurrentAsync(double value)
    {
      var result = await _acwMode.SetArcCurrentAsync(value);
      await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Установка дугового тока ACW", result.Success ? $"{value} мА" : result.Message, result.Success, 1);

      if (!result.Success)
        throw AcwExceptionFactory.SetArcCurrentFailed(_device.Name, _device.NumberChassis, _device.Number, result.Message);

      return result;
    }

    /// <inheritdoc />
    public Task<double> GetArcCurrentAsync() => _acwMode.GetArcCurrentAsync();

    #endregion

    #region Конфигурация и измерения

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

    #endregion
  }
}
