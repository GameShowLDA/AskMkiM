using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Errors.Device;
using Ask.Core.Services.UI;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Capabilities;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.UninterruptiblePowerSupply;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using NewCore.Function.Helpers;

namespace NewCore.FunctionAdapters.MikUps1101rRm
{
  /// <summary>
  /// Adapter for UPS power operations with retries and user messages.
  /// </summary>
  internal class PowerManagerAdapter : IPower
  {
    private readonly IUninterruptiblePowerSupply _device;
    private readonly NewCore.Function.MikUps1101rRm.PowerManager _manager;

    public PowerManagerAdapter(IUninterruptiblePowerSupply device)
    {
      _device = device ?? throw new ArgumentNullException(nameof(device));
      _manager = new NewCore.Function.MikUps1101rRm.PowerManager(device);
    }

    /// <inheritdoc />
    public async Task StopPowerAsync(IUserInteractionService? userMessageService = null)
    {
      if (ExecutionConfig.GetIsIdleModeEnabled())
      {
        return;
      }

      var success = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        try
        {
          await _manager.StopPowerAsync(userMessageService);
          if (DeviceDisplayConfig.GetExecutionParametersVisibility())
          {
            await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Отключение питания ИБП", true, 1, userMessageService);
          }

          return true;
        }
        catch (Exception ex)
        {
          await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Отключение питания ИБП", ex.Message, false, 1, userMessageService);
          return false;
        }
      }, userMessageService, deviceTask: true);

      if (!success)
      {
        throw new DeviceException($"Ошибка отключения питания {_device.Name}({_device.NumberChassis}.{_device.Number})");
      }
    }

    /// <inheritdoc />
    public async Task StartPowerAsync(IUserInteractionService? userMessageService = null)
    {
      if (ExecutionConfig.GetIsIdleModeEnabled())
      {
        return;
      }

      var success = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        try
        {
          await _manager.StartPowerAsync(userMessageService);
          if (DeviceDisplayConfig.GetExecutionParametersVisibility())
          {
            await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Включение питания ИБП", true, 1, userMessageService);
          }

          return true;
        }
        catch (Exception ex)
        {
          await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Включение питания ИБП", ex.Message, false, 1, userMessageService);
          return false;
        }
      }, userMessageService, deviceTask: true);

      if (!success)
      {
        throw new DeviceException($"Ошибка включения питания {_device.Name}({_device.NumberChassis}.{_device.Number})");
      }
    }

    /// <inheritdoc />
    public async Task<bool> VerifyPowerAsync(IUserInteractionService? userMessageService = null)
    {
      if (ExecutionConfig.GetIsIdleModeEnabled())
      {
        return true;
      }

      bool result = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        try
        {
          bool powerOk = await _manager.VerifyPowerAsync(userMessageService);
          if (!powerOk || DeviceDisplayConfig.GetExecutionParametersVisibility())
          {
            string message = powerOk ? "Питание в норме" : "Питание отсутствует";
            await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Проверка питания ИБП", message, powerOk, 1, userMessageService);
          }

          return powerOk;
        }
        catch (Exception ex)
        {
          await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Проверка питания ИБП", ex.Message, false, 1, userMessageService);
          return false;
        }
      }, userMessageService, deviceTask: true);

      return result;
    }
  }
}
