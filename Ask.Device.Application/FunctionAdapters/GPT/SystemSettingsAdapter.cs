using Ask.Core.Shared.DTO.Devices.Breakdown;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester.Capabilities;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Device.Runtime.Device;
using Ask.Device.Runtime.Function.GPT;
using Ask.Device.Runtime.Function.Helpers;

namespace Ask.Device.Application.FunctionAdapters.GPT
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

    public async Task SetLcdContrastAsync(double value, IUserInteractionService? userMessageService = null)
    {
      await _systemSettings.SetLcdContrastAsync(value);
      await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Установка контрастности дисплея", $"{value}", true, 1, userMessageService);
    }

    public async Task SetLcdBrightnessAsync(double value, IUserInteractionService? userMessageService = null)
    {
      await _systemSettings.SetLcdBrightnessAsync(value);
      await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Установка яркости дисплея", $"{value}", true, 1, userMessageService);
    }

    public async Task SetBuzzerPrimarySound(bool state, IUserInteractionService? userMessageService = null)
    {
      await _systemSettings.SetBuzzerPrimarySound(state);
      await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Установка звука успешного теста", state ? "ON" : "OFF", true, 1, userMessageService);
    }

    public async Task SetBuzzerFeedbackSound(bool state, IUserInteractionService? userMessageService = null)
    {
      await _systemSettings.SetBuzzerFeedbackSound(state);
      await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Установка звука ошибочного теста", state ? "ON" : "OFF", true, 1, userMessageService);
    }

    public async Task SetBuzzerPrimaryTime(double duration, IUserInteractionService? userMessageService = null)
    {
      await _systemSettings.SetBuzzerPrimaryTime(duration);
      await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Установка длительности успешного сигнала", $"{duration} сек", true, 1, userMessageService);
    }

    public async Task SetBuzzerFeedbackTime(double duration, IUserInteractionService? userMessageService = null)
    {
      await _systemSettings.SetBuzzerFeedbackTime(duration);
      await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Установка длительности ошибочного сигнала", $"{duration} сек", true, 1, userMessageService);
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
