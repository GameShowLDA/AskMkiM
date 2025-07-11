using System;
using System.Threading.Tasks;
using AppConfiguration.Error.Device.Breakdown;
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

    #region Mode

    /// <inheritdoc />
    public async Task<(bool, string)> SetModeAsync()
    {
      var result = await _dcwMode.SetModeAsync();
      await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Установка режима DCW", result.Success ? "DCW" : result.Message, result.Success, 1);

      if (!result.Success)
        throw DcwExceptionFactory.SetModeFailed(_device.Name, _device.NumberChassis, _device.Number, result.Message);

      return result;
    }

    /// <inheritdoc />
    public Task<string> GetModeAsync() => _dcwMode.GetModeAsync();

    #endregion

    #region Voltage

    /// <inheritdoc />
    public async Task<(bool, string)> SetVoltageAsync(double value)
    {
      var result = await _dcwMode.SetVoltageAsync(value);
      await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Установка напряжения DCW", result.Success ? $"{value} В" : result.Message, result.Success, 1);

      if (!result.Success)
        throw DcwExceptionFactory.SetVoltageFailed(_device.Name, _device.NumberChassis, _device.Number, result.Message);

      return result;
    }

    /// <inheritdoc />
    public async Task<double?> GetVoltageAsync()
    {
      try
      {
        var result = await _dcwMode.GetVoltageAsync();
        string message = result.HasValue ? $"{result:F3} кВ" : "Не удалось получить значение напряжения";
        await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Чтение напряжения DCW", message, result.HasValue, 2);
        return result;
      }
      catch (Exception ex)
      {
        await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Ошибка при чтении напряжения DCW", ex.Message, false, 2);
        return null;
      }
    }

    #endregion

    #region HighCurrentLimit

    /// <inheritdoc />
    public async Task<(bool, string)> SetHighCurrentLimitAsync(double value)
    {
      var result = await _dcwMode.SetHighCurrentLimitAsync(value);
      await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Установка верхнего предела тока DCW", result.Success ? $"{value} мА" : result.Message, result.Success, 1);

      if (!result.Success)
        throw DcwExceptionFactory.SetHighLimitFailed(_device.Name, _device.NumberChassis, _device.Number, result.Message);

      return result;
    }

    /// <inheritdoc />
    public Task<double> GetHighCurrentLimitAsync() => _dcwMode.GetHighCurrentLimitAsync();

    #endregion

    #region LowCurrentLimit

    public async Task<(bool, string)> SetLowCurrentLimitAsync(double value)
    {
      var result = await _dcwMode.SetLowCurrentLimitAsync(value);
      await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Установка нижнего предела тока DCW", result.Success ? $"{value} мА" : result.Message, result.Success, 1);

      if (!result.Success)
        throw DcwExceptionFactory.SetLowLimitFailed(_device.Name, _device.NumberChassis, _device.Number, result.Message);

      return result;
    }

    /// <inheritdoc />
    public Task<double> GetLowCurrentLimitAsync() => _dcwMode.GetLowCurrentLimitAsync();

    #endregion

    #region TestTime

    /// <inheritdoc />
    public async Task<(bool, string)> SetTestTimeAsync(double value)
    {
      var result = await _dcwMode.SetTestTimeAsync(value);
      await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Установка времени теста DCW", result.Success ? $"{value} сек" : result.Message, result.Success, 1);

      if (!result.Success)
        throw DcwExceptionFactory.SetTestTimeFailed(_device.Name, _device.NumberChassis, _device.Number, result.Message);

      return result;
    }

    /// <inheritdoc />
    public Task<double> GetTestTimeAsync() => _dcwMode.GetTestTimeAsync();

    #endregion

    #region RampTime

    /// <inheritdoc />
    public async Task<(bool, string)> SetRampTimeAsync(double value)
    {
      var result = await _dcwMode.SetRampTimeAsync(value);
      await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Установка Ramp Time DCW", result.Success ? $"{value} сек" : result.Message, result.Success, 1);

      if (!result.Success)
        throw DcwExceptionFactory.SetRampTimeFailed(_device.Name, _device.NumberChassis, _device.Number, result.Message);

      return result;
    }

    /// <inheritdoc />
    public Task<double> GetRampTimeAsync() => _dcwMode.GetRampTimeAsync();

    #endregion

    #region Offset
    /// <inheritdoc />
    public async Task<(bool, string)> SetOffsetAsync(double value)
    {
      var result = await _dcwMode.SetOffsetAsync(value);
      await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Установка смещения DCW", result.Success ? $"{value} мА" : result.Message, result.Success, 1);

      if (!result.Success)
        throw DcwExceptionFactory.SetOffsetFailed(_device.Name, _device.NumberChassis, _device.Number, result.Message);

      return result;
    }

    /// <inheritdoc />
    public Task<double> GetOffsetAsync() => _dcwMode.GetOffsetAsync();

    #endregion

    #region ArcCurrent
    /// <inheritdoc />
    public async Task<(bool, string)> SetArcCurrentAsync(double value)
    {
      var result = await _dcwMode.SetArcCurrentAsync(value);
      await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Установка дугового тока DCW", result.Success ? $"{value} мА" : result.Message, result.Success, 1);

      if (!result.Success)
        throw DcwExceptionFactory.SetArcCurrentFailed(_device.Name, _device.NumberChassis, _device.Number, result.Message);

      return result;
    }

    /// <inheritdoc />
    public Task<double> GetArcCurrentAsync() => _dcwMode.GetArcCurrentAsync();

    #endregion

    #region Конфигурация и измерение

    /// <inheritdoc />
    public async Task<DcwConfiguration> ReadConfigurationAsync()
    {
      var config = await _dcwMode.ReadConfigurationAsync();
      await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Чтение конфигурации DCW", "Конфигурация считана", true, 1);
      return config;
    }

    /// <inheritdoc />
    public async Task<double> MeasureCurrentAsync(double param = 0)
    {
      try
      {
        double result = await _dcwMode.MeasureCurrentAsync(param);
        await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Измерение тока DCW", $"{result} мА", result >= 0, 2);
        return result;
      }
      catch (Exception ex)
      {
        await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Ошибка измерения тока DCW", ex.Message, false, 2);
        return -1;
      }
    }

    #endregion
  }
}
