using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Errors.Device.Adapters;
using Ask.Core.Services.UI;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using NewCore.Device;
using NewCore.Function.Helpers;

namespace NewCore.FunctionAdapters.GPT
{
  /// <summary>
  /// Адаптер для управления соединением с GPT-79904 с выводом сообщений.
  /// </summary>
  internal class ConnectableManagerAdapter : IConnectable
  {
    private readonly GPT79904 _device;
    private readonly ConnectableManager _manager;
    public event Action DeviceDisponce;
    public event Action IsReset;

    public ConnectableManagerAdapter(GPT79904 device)
    {
      _device = device ?? throw new ArgumentNullException(nameof(device));
      _manager = new ConnectableManager(device);
    }

    public async Task<(bool Connect, string Answer)> ConnectAsync(IUserInteractionService messageService = null)
    {
      if (ExecutionConfig.GetIsIdleModeEnabled())
      {
        return (true, string.Empty);
      }

      var (result, answer) = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        var succes = await _manager.ConnectAsync(messageService);

        if (!succes.Connect || await DeviceDisplayConfig.GetExecutionParametersVisibilityAsync())
        {
          await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Инициализация пробойной установки", string.IsNullOrWhiteSpace(succes.Answer) ? "Успешно" : succes.Answer, succes.Connect, 1, messageService);
        }

        return succes;
      }, messageService, deviceTask: true);

      if (!result)
      {
        throw ConnectionExceptionAdapter.ConnectFailed(_device.Name, _device.NumberChassis, _device.Number, answer);
      }

      return (result, answer);
    }

    public async Task<bool> DisconnectAsync(IUserInteractionService messageService = null)
    {
      if (ExecutionConfig.GetIsIdleModeEnabled())
      {
        return true;
      }

      var result = await UserActionHelper.GetRunWithUserRepeatAsync(() => _manager.DisconnectAsync(), messageService, deviceTask: true);

      if (!result || await DeviceDisplayConfig.GetExecutionParametersVisibilityAsync())
      {
        await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Отключение пробойной установки", result ? "Успешно" : "Ошибка отключения", result, 1, messageService);
      }

      Task.Delay(1000).GetAwaiter().GetResult();
      if (!result)
      {
        throw ConnectionExceptionAdapter.DisconnectFailed(_device.Name, _device.NumberChassis, _device.Number);
      }

      DeviceDisponce?.Invoke();
      return result;
    }

    public async Task<(bool Connect, string Answer)> InitializeAsync(IUserInteractionService messageService = null)
    {
      var (result, answer) = await UserActionHelper.GetRunWithUserRepeatAsync(() => _manager.InitializeAsync(messageService), messageService, deviceTask: true);


      if (!result || await DeviceDisplayConfig.GetExecutionParametersVisibilityAsync())
      {
        await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Инициализация пробойной установки", string.IsNullOrWhiteSpace(answer) ? "ОК" : answer, result, 1, messageService);
      }

      if (!result)
        throw ConnectionExceptionAdapter.InitializeFailed(_device.Name, _device.NumberChassis, _device.Number, answer);

      return (result, answer);
    }

    public async Task<bool> ResetAsync(IUserInteractionService messageService = null)
    {
      var result = await UserActionHelper.GetRunWithUserRepeatAsync(() => _manager.ResetAsync(), messageService, deviceTask: true);

      if (!result)
        throw ConnectionExceptionAdapter.ResetFailed(_device.Name, _device.NumberChassis, _device.Number);

      IsReset?.Invoke();
      return result;
    }

    public string GetConnectionStatus()
    {
      return _manager.GetConnectionStatus();
    }
  }
}
