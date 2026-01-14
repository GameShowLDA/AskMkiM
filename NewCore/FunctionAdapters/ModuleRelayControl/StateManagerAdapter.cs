using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Errors.Device.Adapters;
using Ask.Core.Services.UI;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using NewCore.Function.Helpers;
using NewCore.Function.ModuleRelayControl;

namespace NewCore.FunctionAdapters.ModuleRelayControl
{
  /// <summary>
  /// Адаптер для управления состоянием модуля коммутации реле (МКР) с сообщениями.
  /// </summary>
  internal class StateManagerAdapter : IConnectable
  {
    private readonly Device.ModuleRelayControl _moduleRelayControl;
    private readonly StateManager _stateManager;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="StateManagerAdapter"/>.
    /// </summary>
    /// <param name="moduleRelayControl">Модуль коммутации реле.</param>
    public StateManagerAdapter(Device.ModuleRelayControl moduleRelayControl)
    {
      _moduleRelayControl = moduleRelayControl ?? throw new ArgumentNullException(nameof(moduleRelayControl));
      _stateManager = new StateManager(moduleRelayControl);
    }

    public event Action DeviceDisponce;
    public event Action IsReset;

    /// <inheritdoc />
    public async Task<(bool Connect, string Answer)> ConnectAsync(IUserInteractionService messageService = null)
    {
      return await InitializeAsync(messageService);
    }

    /// <inheritdoc />
    public async Task<bool> DisconnectAsync(IUserInteractionService messageService = null)
    {
      return await ResetAsync(messageService);
    }

    /// <inheritdoc />
    public async Task<(bool Connect, string Answer)> InitializeAsync(IUserInteractionService messageService = null)
    {
      var (result, answer) = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        var answer = await _stateManager.ConnectAsync();

        if (!answer.Connect || await DeviceDisplayConfig.GetExecutionParametersVisibilityAsync())
        {
          await DeviceMessageBuilder.ShowConnectionMessageAsync(_moduleRelayControl, "Инициализация модуля коммутации реле", !answer.Connect ? answer.Answer : string.Empty, answer.Connect, 1, messageService);
        }

        return answer;
      }, messageService);

      if (!result)
      {
        throw ConnectionExceptionAdapter.InitializeFailed(_moduleRelayControl.Name, _moduleRelayControl.NumberChassis, _moduleRelayControl.Number, answer);
      }

      return (result, answer);
    }

    /// <inheritdoc />
    public async Task<bool> ResetAsync(IUserInteractionService messageService = null)
    {
      var result = await UserActionHelper.GetRunWithUserRepeatAsync(() => _stateManager.DisconnectAsync(), messageService, deviceTask: true);

      if (!result || await DeviceDisplayConfig.GetExecutionParametersVisibilityAsync())
      {
        await DeviceMessageBuilder.ShowConnectionMessageAsync(_moduleRelayControl, "Сброс устройства", string.Empty, result, 1, messageService);
      }

      if (!result)
      {
        throw ConnectionExceptionAdapter.ResetFailed(_moduleRelayControl.Name, _moduleRelayControl.NumberChassis, _moduleRelayControl.Number, "Ошибка выполнения команды"); ;
      }

      IsReset?.Invoke();
      return result;
    }
  }
}