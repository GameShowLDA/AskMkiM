using Ask.Core.Services.Errors.Device.DeviceBusCommutation;
using Ask.Core.Services.UI;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice.Capabilities;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using NewCore.Function.DeviceBusCommutation;
using NewCore.Function.Helpers;

namespace NewCore.FunctionAdapters.DeviceBusCommutation
{
  /// <summary>
  /// Адаптер управления подключением/отключением реле.
  /// </summary>
  internal class RelayManagerAdapter : IRelayDeviceBusCommutation
  {
    private readonly Device.DeviceBusCommutation _deviceBusCommutation;
    private readonly RelayManager _relayManager;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="RelayManagerAdapter"/>.
    /// </summary>
    /// <param name="deviceBusCommutation">Экземпляр устройства коммутации шин.</param>
    public RelayManagerAdapter(Device.DeviceBusCommutation deviceBusCommutation)
    {
      _deviceBusCommutation = deviceBusCommutation ?? throw new ArgumentNullException(nameof(deviceBusCommutation));
      _relayManager = new RelayManager(deviceBusCommutation);
    }

    /// <inheritdoc />
    public async Task<bool> ConnectRelay(int numberRelay, IUserInteractionService? userMessageService = null)
    {
      var result = await UserActionHelper.GetRunWithUserRepeatAsync(() => _relayManager.ConnectRelay(numberRelay), userMessageService);

      await DeviceMessageBuilder.ShowConnectionMessageAsync(_deviceBusCommutation, "Подключение реле", $"№{numberRelay}", result, 1, userMessageService);

      if (!result)
        throw RelayControlExceptionFactory.ConnectFailed(numberRelay);

      return result;
    }

    /// <inheritdoc />
    public async Task<bool> DisconnectRelay(int numberRelay, IUserInteractionService? userMessageService = null)
    {
      var result = await UserActionHelper.GetRunWithUserRepeatAsync(() => _relayManager.DisconnectRelay(numberRelay), userMessageService);

      await DeviceMessageBuilder.ShowConnectionMessageAsync(_deviceBusCommutation, "Отключение реле", $"№{numberRelay}", result, 1, userMessageService);

      if (!result)
        throw RelayControlExceptionFactory.DisconnectFailed(numberRelay);

      return result;
    }
  }
}
