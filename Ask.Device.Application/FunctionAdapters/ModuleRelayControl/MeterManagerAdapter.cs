using Ask.Core.Services.Errors.Device.ModuleRelayControl;
using Ask.Core.Services.UI;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule.Capabilities;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Device.Runtime.Function.ModuleRelayControl;

namespace Ask.Device.Application.FunctionAdapters.ModuleRelayControl
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

      var result = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        return await _meterManager.ConnectMeterAsync(userMessageService);
      }, userMessageService, deviceTask: true);

      if (!result)
      {
        const string description = "модуля МКР";
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
      if (!IsConnectMeter)
        return true;

      const string description = "модуля МКР";

      var result = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        return await _meterManager.DisconnectMeterAsync(userMessageService);
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
      return await UserActionHelper.GetRunWithUserRepeatAsync(
        () => _meterManager.GetMeterResponseAsync(userMessageService),
        static _ => true,
        userMessageService,
        deviceTask: true);
    }
  }
}
