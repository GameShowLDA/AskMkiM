using System;
using System.Threading.Tasks;
using NewCore.Function.Helpers;
using NewCore.Function.ModuleRelayControl;
using NewCore.Base.Interface.Main;
using NewCore.Base.Device;

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
    public async Task<(bool Connect, string Answer)> ConnectAsync()
    {
      return await InitializeAsync();
    }

    /// <inheritdoc />
    public async Task<bool> DisconnectAsync()
    {
      return await ResetAsync();
    }

    /// <inheritdoc />
    public async Task<(bool Connect, string Answer)> InitializeAsync()
    {
      var (result, answer) = await _stateManager.ConnectAsync();

      await DeviceMessageBuilder.ShowConnectionMessageAsync(
        _moduleRelayControl,
        "Инициализация модуля коммутации реле",
        answer,
        result, 
        1);

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

      return result;
    }
  }
}
