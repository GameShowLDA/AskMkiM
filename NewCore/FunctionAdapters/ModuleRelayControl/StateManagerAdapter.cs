using System;
using System.Threading.Tasks;
using NewCore.Function.Helpers;
using NewCore.Function.ModuleRelayControl;
using NewCore.Base.Interface.Main;
using NewCore.Base.Device;
using Utilities.Error.Device;
using NewCore.Base.Function.ModuleRelayControl;

namespace NewCore.FunctionAdapters.ModuleRelayControl
{
  /// <summary>
  /// Адаптер для управления состоянием модуля коммутации реле (МКР) с сообщениями.
  /// </summary>
  internal class StateManagerAdapter : NewCore.Base.Function.ModuleRelayControl.IStateManager
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
    public async Task<(bool Connect, string Answer)> ConnectAsync()
    {
      return await Initialize();
    }

    /// <inheritdoc />
    public async Task<bool> DisconnectAsync()
    {
      return await ResetAsync();
    }

    public async Task<(bool Connect, string Answer)> Initialize()
    {
      var (result, answer) = await _stateManager.ConnectAsync();

      await DeviceMessageBuilder.ShowConnectionMessageAsync(
          _moduleRelayControl,
          "Инициализация модуля коммутации реле",
          !result ? answer : string.Empty,
          result,
          1);

      if (!result)
        throw ConnectionExceptionFactory.InitializeFailed(_moduleRelayControl.Name, _moduleRelayControl.NumberChassis, _moduleRelayControl.Number, answer);

      return (result, answer);
    }

    /// <inheritdoc />
    public async Task<bool> ResetAsync()
    {
      var result = await _stateManager.DisconnectAsync();

      await DeviceMessageBuilder.ShowConnectionMessageAsync(
          _moduleRelayControl,
          "Сброс модуля коммутации реле",
          result ? "Операция выполнена успешно" : "Ошибка выполнения команды",
          result,
          1);

      if (!result)
        throw ConnectionExceptionFactory.ResetFailed(_moduleRelayControl.Name, _moduleRelayControl.NumberChassis, _moduleRelayControl.Number, "Ошибка выполнения команды");

      return result;
    }

    Task IStateManager.ResetAsync()
    {
      return ResetAsync();
    }
  }
}
