using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Errors.Device;
using Ask.Core.Shared.DTO.Devices.Breakdown;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester.Capabilities;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Services.UI;
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
      await ExecuteSettingAsync(
        () => _systemSettings.SetLcdContrastAsync(value),
        "Установка контрастности дисплея",
        $"{value}",
        userMessageService);
    }

    public async Task SetLcdBrightnessAsync(double value, IUserInteractionService? userMessageService = null)
    {
      await ExecuteSettingAsync(
        () => _systemSettings.SetLcdBrightnessAsync(value),
        "Установка яркости дисплея",
        $"{value}",
        userMessageService);
    }

    public async Task SetBuzzerPrimarySound(bool state, IUserInteractionService? userMessageService = null)
    {
      await ExecuteSettingAsync(
        () => _systemSettings.SetBuzzerPrimarySound(state),
        "Установка звука успешного теста",
        state ? "ON" : "OFF",
        userMessageService);
    }

    public async Task SetBuzzerFeedbackSound(bool state, IUserInteractionService? userMessageService = null)
    {
      await ExecuteSettingAsync(
        () => _systemSettings.SetBuzzerFeedbackSound(state),
        "Установка звука ошибочного теста",
        state ? "ON" : "OFF",
        userMessageService);
    }

    public async Task SetBuzzerPrimaryTime(double duration, IUserInteractionService? userMessageService = null)
    {
      await ExecuteSettingAsync(
        () => _systemSettings.SetBuzzerPrimaryTime(duration),
        "Установка длительности успешного сигнала",
        $"{duration} сек",
        userMessageService);
    }

    public async Task SetBuzzerFeedbackTime(double duration, IUserInteractionService? userMessageService = null)
    {
      await ExecuteSettingAsync(
        () => _systemSettings.SetBuzzerFeedbackTime(duration),
        "Установка длительности ошибочного сигнала",
        $"{duration} сек",
        userMessageService);
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

    /// <summary>
    /// Применяет системную настройку GPT с поддержкой повторных попыток.
    /// </summary>
    /// <param name="operation">Операция изменения настройки.</param>
    /// <param name="operationName">Название операции.</param>
    /// <param name="value">Устанавливаемое значение.</param>
    /// <param name="userMessageService">Сервис взаимодействия с пользователем.</param>
    private async Task ExecuteSettingAsync(
      Func<Task> operation,
      string operationName,
      string value,
      IUserInteractionService? userMessageService)
    {
      bool success = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        try
        {
          await operation();
          await DeviceMessageBuilder.ShowConnectionMessageAsync(
            _device,
            operationName,
            value,
            true,
            1,
            userMessageService);
          return true;
        }
        catch (Exception ex) when (ExecutionConfig.GetIsIdleModeEnabled())
        {
          await DeviceMessageBuilder.ShowConnectionMessageAsync(
            _device,
            operationName,
            ex.Message,
            false,
            1,
            userMessageService);
          return false;
        }
      }, ExecutionConfig.GetIsIdleModeEnabled() ? userMessageService : null, deviceTask: true);

      if (!success)
      {
        throw new DeviceException(IdleHardwareErrorSimulator.ErrorMessage);
      }
    }
  }
}
