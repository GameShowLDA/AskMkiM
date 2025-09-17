using NewCore.Base.Function.Breakdown;
using NewCore.Device;
using NewCore.Function.GPT;
using NewCore.Function.GPT.Data;
using NewCore.Function.Helpers;

namespace NewCore.FunctionAdapters.GPT
{
  /// <summary>
  /// Адаптер системных настроек устройства GPT-79904 с отображением сообщений.
  /// </summary>
  internal class SystemSettingsAdapter : ISystemSettingsBreakdown
  {
    private readonly GPT79904 _device;
    private readonly SystemSettings _systemSettings;

    public SystemSettingsAdapter(GPT79904 device)
    {
      _device = device ?? throw new ArgumentNullException(nameof(device));
      _systemSettings = new SystemSettings(device);
    }

    public async Task SetLcdContrastAsync(double value)
    {
      await _systemSettings.SetLcdContrastAsync(value);
      await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Установка контрастности дисплея", $"{value}", true, 1);
    }

    public async Task SetLcdBrightnessAsync(double value)
    {
      await _systemSettings.SetLcdBrightnessAsync(value);
      await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Установка яркости дисплея", $"{value}", true, 1);
    }

    public async Task SetBuzzerPrimarySound(bool state)
    {
      await _systemSettings.SetBuzzerPrimarySound(state);
      await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Установка звука успешного теста", state ? "ON" : "OFF", true, 1);
    }

    public async Task SetBuzzerFeedbackSound(bool state)
    {
      await _systemSettings.SetBuzzerFeedbackSound(state);
      await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Установка звука ошибочного теста", state ? "ON" : "OFF", true, 1);
    }

    public async Task SetBuzzerPrimaryTime(double duration)
    {
      await _systemSettings.SetBuzzerPrimaryTime(duration);
      await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Установка длительности успешного сигнала", $"{duration} сек", true, 1);
    }

    public async Task SetBuzzerFeedbackTime(double duration)
    {
      await _systemSettings.SetBuzzerFeedbackTime(duration);
      await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Установка длительности ошибочного сигнала", $"{duration} сек", true, 1);
    }

    public async Task<SystemDataModel> ReadConfigurationAsync()
    {
      var config = await _systemSettings.ReadConfigurationAsync();
      await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Чтение конфигурации системных настроек", "Конфигурация считана", true, 1);
      return config;
    }

    public async Task<bool> TestReset()
    {
      if (await _systemSettings.TestReset())
      {
        var result = await _device.ConnectableManager.InitializeAsync();
        return result.Connect;
      }

      return false;
    }
  }
}
