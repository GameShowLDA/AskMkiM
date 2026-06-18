using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.UI;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Device.Application.Function.Helpers;
using Ask.Device.Runtime.Device;
using Ask.Device.Runtime.Function.B7783;
using Ask.Device.Runtime.Function.Helpers;

namespace Ask.Device.Application.FunctionAdapters.B7783
{
  internal class StateManagerAdapter : IConnectable
  {
    private readonly MultimeterB7783 _device;
    private readonly StateManager _stateManager;

    public StateManagerAdapter(MultimeterB7783 device)
    {
      _device = device ?? throw new ArgumentNullException(nameof(device));
      _stateManager = new StateManager(device);
    }

    public event Action IsReset;

    public async Task<(bool Connect, string Answer)> InitializeAsync(IUserInteractionService userMessageService = null)
    {
      return await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        var result = await _stateManager.InitializeAsync(userMessageService);

        if (!result.Connect || DeviceDisplayConfig.GetExecutionParametersVisibility())
        {
          await DeviceMessageBuilder.ShowConnectionMessageAsync(
            _device,
            "Инициализация мультиметра В7-78/3",
            result.Answer,
            result.Connect,
            1,
            userMessageService);
        }

        return result;
      }, userMessageService);
    }

    public async Task<(bool Connect, string Answer)> ConnectAsync(IUserInteractionService userMessageService = null)
    {
      return await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        var result = await _stateManager.ConnectAsync(userMessageService);

        if (!result.Connect || DeviceDisplayConfig.GetExecutionParametersVisibility())
        {
          await DeviceMessageBuilder.ShowConnectionMessageAsync(
            _device,
            "Подключение мультиметра В7-78/3",
            result.Answer,
            result.Connect,
            1,
            userMessageService);
        }

        return result;
      }, userMessageService);
    }

    public async Task<bool> DisconnectAsync(IUserInteractionService userMessageService = null)
    {
      bool result = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        bool disconnected = await _stateManager.DisconnectAsync(userMessageService);

        if (!disconnected || DeviceDisplayConfig.GetExecutionParametersVisibility())
        {
          await DeviceMessageBuilder.ShowConnectionMessageAsync(
            _device,
            "Отключение мультиметра В7-78/3",
            disconnected ? "Соединение разорвано" : "Ошибка отключения",
            disconnected,
            1,
            userMessageService);
        }

        return disconnected;
      }, userMessageService);

      IsReset?.Invoke();
      return result;
    }

    public async Task<bool> ResetAsync(IUserInteractionService userMessageService = null)
    {
      bool result = await UserActionHelper.GetRunWithUserRepeatAsync(
        () => _stateManager.ResetAsync(userMessageService),
        userMessageService);

      IsReset?.Invoke();
      return result;
    }

    public string GetConnectionStatus() => _stateManager.GetConnectionStatus();
  }
}
