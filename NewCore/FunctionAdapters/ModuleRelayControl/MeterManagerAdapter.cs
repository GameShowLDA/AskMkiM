using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Errors.Device.ModuleRelayControl;
using Ask.Core.Services.UI;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule.Capabilities;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using NewCore.Function.Helpers;
using NewCore.Function.ModuleRelayControl;

namespace NewCore.FunctionAdapters.ModuleRelayControl
{
  /// <summary>
  /// Адаптер для управления измерителем модуля МКР с отображением сообщений.
  /// </summary>
  internal class MeterManagerAdapter : IMeterManager
  {
    private readonly IRelaySwitchModule _moduleRelayControl;
    private readonly MeterManager _meterManager;
    private bool IsConnectMeter = false;
    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="MeterManagerAdapter"/>.
    /// </summary>
    /// <param name="moduleRelayControl">Экземпляр модуля реле.</param>
    public MeterManagerAdapter(IRelaySwitchModule moduleRelayControl)
    {
      _moduleRelayControl = moduleRelayControl ?? throw new ArgumentNullException(nameof(moduleRelayControl));
      _meterManager = new MeterManager(moduleRelayControl);
      IsConnectMeter = false;

      moduleRelayControl.ConnectableManager.IsReset += () => IsConnectMeter = false;
    }

    /// <inheritdoc />
    public async Task<bool> ConnectMeterAsync(IUserInteractionService? userMessageService = null)
    {
      if (IsConnectMeter)
        return true;

      const string description = "модуля МКР";
      var result = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        var succes = await _meterManager.ConnectMeterAsync();

        if (!succes || DeviceDisplayConfig.GetConnectionInfoVisibility())
        {
          await DeviceMessageBuilder.ShowConnectionMessageAsync(_moduleRelayControl, $"Подключение измерителя {description}", succes, 1, userMessageService);
        }

        return succes;
      }, userMessageService, deviceTask: true);

      if (!result)
      {
        throw MeterExceptionFactory.ConnectFailed(description);
      }
      else
      {
        IsConnectMeter = true;
      }

      return result;
    }

    /// <inheritdoc />
    public async Task<bool> DisconnectMeterAsync(IUserInteractionService? userMessageService = null)
    {
      if (IsConnectMeter)
        return false;

      const string description = "модуля МКР";

      var result = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        var succes = await _meterManager.DisconnectMeterAsync();

        if (!succes || DeviceDisplayConfig.GetConnectionInfoVisibility())
        {
          await DeviceMessageBuilder.ShowConnectionMessageAsync(_moduleRelayControl, $"Отключение измерителя {description}", succes, 1, userMessageService);
        }

        return succes;
      }, userMessageService, deviceTask: true);


      if (!result)
      {
        throw MeterExceptionFactory.DisconnectFailed(description);
      }
      else
      {
        IsConnectMeter = false;
      }

      return result;
    }

    /// <inheritdoc />
    public async Task<bool> GetMeterResponseAsync(IUserInteractionService? userMessageService = null)
    {
      return await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
       {
         var succes = await _meterManager.GetMeterResponseAsync();
         await DeviceMessageBuilder.ShowConnectionMessageAsync(
           _moduleRelayControl,
           succes ? "Обнаружено подлючение шин/точек (МКР)" : "Подлючение шин/точек не обнаружено (МКР)",
           succes,
           1,
           userMessageService);

         if (!succes)
         {
           throw MeterExceptionFactory.MeterAnswerFailed(_moduleRelayControl.Name, _moduleRelayControl.NumberChassis, _moduleRelayControl.Number);
         }

         return succes;

       }, userMessageService, deviceTask: true);
    }
  }
}
