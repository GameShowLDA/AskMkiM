using System;
using System.Threading.Tasks;
using NewCore.Base.Device;
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
      var (connect, answer) = await _stateManager.ConnectAsync();
      await DeviceMessageBuilder.ShowConnectionMessageAsync(
        _deviceBusCommutation,
        "Инициализация устройства",
        answer,
        connect, 
        1);
      return (connect, answer);
    }

    /// <inheritdoc />
    public async Task<bool> ResetAsync()
    {
      var result = await _stateManager.DisconnectAsync();
      await DeviceMessageBuilder.ShowConnectionMessageAsync(
        _deviceBusCommutation,
        "Сброс устройства",
        result ? "Операция выполнена успешно" : "Операция завершилась с ошибкой",
        result, 
        1);
      return result;
    }
  }
}
