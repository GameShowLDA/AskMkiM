using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Errors.Device.Adapters;
using Ask.Core.Services.UI;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.PowerSourceModule;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Device.Runtime.Function.Helpers;
using Ask.Device.Runtime.Function.ModuleVoltageCurrentSource;

namespace Ask.Device.Application.FunctionAdapters.ModuleVoltageCurrent
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
      var (result, answer) = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        var (success, message) = await _stateManager.ConnectAsync();

        if (!success || DeviceDisplayConfig.GetExecutionParametersVisibility())
        {
          await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Подключение", message, success, 1, messageService);
        }

        return (success, message);
      }, messageService);

      if (!result)
      {
        throw ConnectionExceptionAdapter.ConnectFailed(_device.Name, _device.NumberChassis, _device.Number, answer);
      }

      return (result, answer);
    }

    /// <inheritdoc />
    public async Task<bool> DisconnectAsync(IUserInteractionService messageService = null)
    {
      var result = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        var success = await _stateManager.DisconnectAsync();

        if (!success || DeviceDisplayConfig.GetExecutionParametersVisibility())
        {
          await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Отключение", success, 1);
        }

        return success;
      }, messageService);

      if (!result)
      {
        throw ConnectionExceptionAdapter.DisconnectFailed(_device.Name, _device.NumberChassis, _device.Number);
      }

      return result;
    }

    public string GetConnectionStatus()
    {
      throw new NotImplementedException();
    }

    /// <inheritdoc />
    public async Task<(bool Connect, string Answer)> InitializeAsync(IUserInteractionService messageService = null)
    {
      var (result, answer) = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        var (success, message) = await _stateManager.InitializeAsync();

        if (!success || DeviceDisplayConfig.GetExecutionParametersVisibility())
        {
          await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Инициализация", message, success, 1, messageService);
        }

        return (success, message);
      }, messageService);

      if (!result)
      {
        throw ConnectionExceptionAdapter.InitializeFailed(_device.Name, _device.NumberChassis, _device.Number, answer);
      }

      return (result, answer);
    }

    /// <inheritdoc />
    public async Task<bool> ResetAsync(IUserInteractionService messageService = null)
    {
      var result = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        var success = await _stateManager.ResetAsync();

        if (!success || DeviceDisplayConfig.GetExecutionParametersVisibility())
        {
          await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Сброс", success, 1);
        }

        return success;
      }, messageService);

      if (!result)
      {
        throw ConnectionExceptionAdapter.ResetFailed(_device.Name, _device.NumberChassis, _device.Number);
      }

      IsReset?.Invoke();
      return result;
    }
  }
}
