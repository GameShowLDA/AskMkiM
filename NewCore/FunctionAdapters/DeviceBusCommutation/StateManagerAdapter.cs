using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Errors.Device.Adapters;
using Ask.Core.Services.UI;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using NewCore.Function.DeviceBusCommutation;
using NewCore.Function.Helpers;
using System.Text;

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

    public event Action DeviceDisponce;
    public event Action IsReset;

    /// <inheritdoc />
    public async Task<(bool Connect, string Answer)> ConnectAsync(IUserInteractionService userMessageService = null)
    {
      return await InitializeAsync(userMessageService);
    }

    /// <inheritdoc />
    public async Task<bool> DisconnectAsync(IUserInteractionService userMessageService = null)
    {
      return await ResetAsync(userMessageService);
    }

    public string GetConnectionStatus()
    {
      var devices = _deviceBusCommutation.ConnectorManager.GetConnectedDevices();

      if (devices.Count() == 0)
        return "Подключенные устройства:\n  Нет подключённых устройств.";

      var sb = new StringBuilder();
      sb.AppendLine("Подключенные устройства:");

      foreach (var d in devices)
      {
        sb.AppendLine($"  {d.device} — {d.bus}");
      }

      return sb.ToString();
    }

    /// <inheritdoc />
    public async Task<(bool Connect, string Answer)> InitializeAsync(IUserInteractionService userMessageService = null)
    {
      (bool connect, string answer) = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        var result = await _stateManager.ConnectAsync();

        if (!result.Connect || await DeviceDisplayConfig.GetExecutionParametersVisibilityAsync())
        {
          await DeviceMessageBuilder.ShowConnectionMessageAsync(_deviceBusCommutation, "Инициализация устройства", !result.Connect ? result.Answer : string.Empty, result.Connect, 1, userMessageService);
        }

        return result;
      }, userMessageService, deviceTask: true);

      if (!connect)
        throw ConnectionExceptionAdapter.InitializeFailed(_deviceBusCommutation.Name, _deviceBusCommutation.NumberChassis, _deviceBusCommutation.Number, answer);

      return (connect, answer);
    }

    /// <inheritdoc />
    public async Task<bool> ResetAsync(IUserInteractionService userMessageService = null)
    {
      var result = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        var succes = await _stateManager.DisconnectAsync();

        if (!succes || await DeviceDisplayConfig.GetExecutionParametersVisibilityAsync())
        {
          await DeviceMessageBuilder.ShowConnectionMessageAsync(_deviceBusCommutation, "Сброс устройства", succes, 1, userMessageService);
        }

        return succes;
      }, userMessageService, deviceTask: true);

      if (!result)
        throw ConnectionExceptionAdapter.ResetFailed(_deviceBusCommutation.Name, _deviceBusCommutation.NumberChassis, _deviceBusCommutation.Number);

      IsReset?.Invoke();
      return result;
    }
  }
}
