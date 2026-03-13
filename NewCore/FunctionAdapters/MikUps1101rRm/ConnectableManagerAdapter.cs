using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Errors.Device.Adapters;
using Ask.Core.Services.UI;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.UninterruptiblePowerSupply;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using NewCore.Function.Helpers;

namespace NewCore.FunctionAdapters.MikUps1101rRm
{
  /// <summary>
  /// Adapter for UPS connection workflow with retries and user messages.
  /// </summary>
  internal class ConnectableManagerAdapter : IConnectable
  {
    private readonly IUninterruptiblePowerSupply _device;
    private readonly NewCore.Function.MikUps1101rRm.ConnectableManager _manager;

    public ConnectableManagerAdapter(IUninterruptiblePowerSupply device)
    {
      _device = device ?? throw new ArgumentNullException(nameof(device));
      _manager = new NewCore.Function.MikUps1101rRm.ConnectableManager(device);
    }

    public event Action IsReset
    {
      add => _manager.IsReset += value;
      remove => _manager.IsReset -= value;
    }

    /// <inheritdoc />
    public async Task<(bool Connect, string Answer)> InitializeAsync(IUserInteractionService messageService = null)
    {
      var (result, answer) = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        var state = await _manager.InitializeAsync(messageService);
        if (!state.Connect || DeviceDisplayConfig.GetExecutionParametersVisibility())
        {
          await DeviceMessageBuilder.ShowConnectionMessageAsync(
            _device,
            "Инициализация бесперебойника",
            string.IsNullOrWhiteSpace(state.Answer) ? "OK" : state.Answer,
            state.Connect,
            1,
            messageService);
        }

        return state;
      }, messageService, deviceTask: true);

      if (!result)
      {
        throw ConnectionExceptionAdapter.InitializeFailed(_device.Name, _device.NumberChassis, _device.Number, answer);
      }

      return (result, answer);
    }

    /// <inheritdoc />
    public async Task<(bool Connect, string Answer)> ConnectAsync(IUserInteractionService messageService = null)
    {
      var (result, answer) = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        var state = await _manager.ConnectAsync(messageService);
        if (!state.Connect || DeviceDisplayConfig.GetExecutionParametersVisibility())
        {
          await DeviceMessageBuilder.ShowConnectionMessageAsync(
            _device,
            "Подключение бесперебойника",
            string.IsNullOrWhiteSpace(state.Answer) ? "OK" : state.Answer,
            state.Connect,
            1,
            messageService);
        }

        return state;
      }, messageService, deviceTask: true);

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
        var success = await _manager.DisconnectAsync(messageService);
        if (!success || DeviceDisplayConfig.GetExecutionParametersVisibility())
        {
          await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Отключение бесперебойника", success, 1, messageService);
        }

        return success;
      }, messageService, deviceTask: true);

      if (!result)
      {
        throw ConnectionExceptionAdapter.DisconnectFailed(_device.Name, _device.NumberChassis, _device.Number);
      }

      return result;
    }

    /// <inheritdoc />
    public async Task<bool> ResetAsync(IUserInteractionService messageService = null)
    {
      var result = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        var success = await _manager.ResetAsync(messageService);
        if (!success || DeviceDisplayConfig.GetExecutionParametersVisibility())
        {
          await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Сброс бесперебойника", success, 1, messageService);
        }

        return success;
      }, messageService, deviceTask: true);

      if (!result)
      {
        throw ConnectionExceptionAdapter.ResetFailed(_device.Name, _device.NumberChassis, _device.Number);
      }

      return result;
    }

    /// <inheritdoc />
    public string GetConnectionStatus()
    {
      return _manager.GetConnectionStatus();
    }
  }
}
