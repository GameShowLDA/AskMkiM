using AppConfiguration.Error.Device;
using NewCore.Base.Device;
using NewCore.Function.Helpers;
using NewCore.Function.ModuleRelayControl;
using Utilities;
using Utilities.Interface;

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

    /// <inheritdoc />
    public async Task<(bool Connect, string Answer)> ConnectAsync(IUserMessageService messageService = null)
    {
      return await InitializeAsync(messageService);
    }

    /// <inheritdoc />
    public async Task<bool> DisconnectAsync(IUserMessageService messageService = null)
    {
      return await ResetAsync(messageService);
    }

    /// <inheritdoc />
    public async Task<(bool Connect, string Answer)> InitializeAsync(IUserMessageService messageService = null)
    {
      var (result, answer) = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        var answer = await _stateManager.ConnectAsync();

        await DeviceMessageBuilder.ShowConnectionMessageAsync(
            _moduleRelayControl,
            "Инициализация модуля коммутации реле",
            !answer.Connect ? answer.Answer : string.Empty,
            answer.Connect,
            1);
        return answer;
      }, messageService);


      if (!result)
      {
        var error = ConnectionExceptionFactory.InitializeFailed(_moduleRelayControl.Name, _moduleRelayControl.NumberChassis, _moduleRelayControl.Number, answer);
        if (error != null)
        {
          throw error;
        }
        else
        {
          result = true;
        }
      }

      return (result, answer);
    }

    /// <inheritdoc />
    public async Task<bool> ResetAsync(IUserMessageService messageService = null)
    {
      var result = await _stateManager.DisconnectAsync();

      await DeviceMessageBuilder.ShowConnectionMessageAsync(
          _moduleRelayControl,
          "Сброс устройства",
         string.Empty,
          result,
          1);

      if (!result)
      {
        var error = ConnectionExceptionFactory.ResetFailed(_moduleRelayControl.Name, _moduleRelayControl.NumberChassis, _moduleRelayControl.Number, "Ошибка выполнения команды");
        if (error != null)
        {
          throw error;
        }
        else
        {
          result = true;
        }
      }

      return result;
    }
  }
}