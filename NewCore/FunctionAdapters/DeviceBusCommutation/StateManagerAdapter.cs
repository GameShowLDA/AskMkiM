using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Errors.Device.Adapters;
using Ask.Core.Services.UI;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using NewCore.Function.DeviceBusCommutation;
using NewCore.Function.Helpers;

namespace NewCore.FunctionAdapters.DeviceBusCommutation
{
  /// <summary>
  /// Адаптер для управления состоянием устройства коммутации.
  /// </summary>
  internal class StateManagerAdapter : IConnectable
  {
    private readonly Device.DeviceBusCommutation _deviceBusCommutation;
    private readonly StateManager _stateManager;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="StateManagerAdapter"/>.
    /// </summary>
    /// <param name="deviceBusCommutation">Устройство коммутации шин.</param>
    public StateManagerAdapter(Device.DeviceBusCommutation deviceBusCommutation)
    {
      _deviceBusCommutation = deviceBusCommutation ?? throw new ArgumentNullException(nameof(deviceBusCommutation));
      _stateManager = new StateManager(deviceBusCommutation);
    }

    public event Action DeviceDisponce;
    public event Action IsReset;

    /// <inheritdoc />
    public async Task<(bool Connect, string Answer)> ConnectAsync(IUserInteractionService userMessageService = null)
    {
      return await InitializeAsync(userMessageService);
    }

    /// <inheritdoc />
    public async Task<bool> DisconnectAsync(IUserInteractionService userMessageService = null)
    {
      return await ResetAsync(userMessageService);
    }

    /// <inheritdoc />
    public async Task<(bool Connect, string Answer)> InitializeAsync(IUserInteractionService userMessageService = null)
    {
      (bool connect, string answer) = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        var result = await _stateManager.ConnectAsync();

        if (!result.Connect || await DeviceDisplayConfig.GetExecutionParametersVisibilityAsync())
        {
          await DeviceMessageBuilder.ShowConnectionMessageAsync(_deviceBusCommutation, "Инициализация устройства", !result.Connect ? result.Answer : string.Empty, result.Connect, 1, userMessageService);
        }

        return result;
      }, userMessageService);

      if (!connect)
      {
        var error = ConnectionExceptionAdapter.InitializeFailed(_deviceBusCommutation.Name, _deviceBusCommutation.NumberChassis, _deviceBusCommutation.Number, answer);
        if (error != null)
        {
          throw error;
        }
        else
        {
          connect = true;
        }
      }

      return (connect, answer);
    }

    /// <inheritdoc />
    public async Task<bool> ResetAsync(IUserInteractionService userMessageService = null)
    {
      var result = await _stateManager.DisconnectAsync();

      if (!result || await DeviceDisplayConfig.GetExecutionParametersVisibilityAsync())
      {
        await DeviceMessageBuilder.ShowConnectionMessageAsync(_deviceBusCommutation, "Сброс устройства", result, 1, userMessageService);
      }

      IsReset?.Invoke();
      return result;
    }
  }
}
