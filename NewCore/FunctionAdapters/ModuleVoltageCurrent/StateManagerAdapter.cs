using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Errors.Device.Adapters;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.PowerSourceModule;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using NewCore.Function.Helpers;
using NewCore.Function.ModuleVoltageCurrentSource;

namespace NewCore.FunctionAdapters.ModuleVoltageCurrent
{
  /// <summary>
  /// Адаптер для управления состоянием МИНТ с отображением сообщений.
  /// </summary>
  internal class StateManagerAdapter : IConnectable
  {
    private readonly IPowerSourceModule _device;
    private readonly StateManager _stateManager;

    public StateManagerAdapter(IPowerSourceModule device)
    {
      _device = device ?? throw new ArgumentNullException(nameof(device));
      _stateManager = new StateManager(device);
    }

    public event Action DeviceDisponce;
    public event Action IsReset;

    /// <inheritdoc />
    public async Task<(bool Connect, string Answer)> ConnectAsync(IUserInteractionService messageService = null)
    {
      var (success, message) = await _stateManager.ConnectAsync();

      if (!success || await DeviceDisplayConfig.GetExecutionParametersVisibilityAsync())
      {
        await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Подключение", message, success, 1, messageService);
      }

      if (!success)
        throw ConnectionExceptionAdapter.ConnectFailed(_device.Name, _device.NumberChassis, _device.Number, message);

      return (success, message);
    }

    /// <inheritdoc />
    public async Task<bool> DisconnectAsync(IUserInteractionService messageService = null)
    {
      bool success = await _stateManager.DisconnectAsync();

      if (!success || await DeviceDisplayConfig.GetExecutionParametersVisibilityAsync())
      {
        await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Отключение", success, 1);
      }

      if (!success)
        throw ConnectionExceptionAdapter.DisconnectFailed(_device.Name, _device.NumberChassis, _device.Number);

      return success;
    }

    /// <inheritdoc />
    public async Task<(bool Connect, string Answer)> InitializeAsync(IUserInteractionService messageService = null)
    {
      var (success, message) = await _stateManager.InitializeAsync();

      if (!success || await DeviceDisplayConfig.GetExecutionParametersVisibilityAsync())
      {
        await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Инициализация", message, success, 1, messageService);
      }

      if (!success)
        throw ConnectionExceptionAdapter.InitializeFailed(_device.Name, _device.NumberChassis, _device.Number, message);

      return (success, message);
    }

    /// <inheritdoc />
    public async Task<bool> ResetAsync(IUserInteractionService messageService = null)
    {
      bool success = await _stateManager.ResetAsync();

      if (!success || await DeviceDisplayConfig.GetExecutionParametersVisibilityAsync())
      {
        await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Сброс", success, 1);
      }

      if (!success)
        throw ConnectionExceptionAdapter.ResetFailed(_device.Name, _device.NumberChassis, _device.Number);

      IsReset?.Invoke();
      return success;
    }
  }
}
