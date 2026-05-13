using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Errors.Device.DeviceBusCommutation;
using Ask.Core.Services.UI;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice.Capabilities;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Device.Runtime.Function.DeviceBusCommutation;
using Ask.Device.Runtime.Function.Helpers;

namespace Ask.Device.Application.FunctionAdapters.DeviceBusCommutation
{
  /// <summary>
  /// Адаптер управления подключением/отключением реле.
  /// </summary>
  internal class RelayManagerAdapter : IRelayDeviceBusCommutation
  {
    private readonly Runtime.Device.DeviceBusCommutation _deviceBusCommutation;
    private readonly RelayManager _relayManager;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="RelayManagerAdapter"/>.
    /// </summary>
    /// <param name="deviceBusCommutation">Экземпляр устройства коммутации шин.</param>
    public RelayManagerAdapter(Runtime.Device.DeviceBusCommutation deviceBusCommutation)
    {
      _deviceBusCommutation = deviceBusCommutation ?? throw new ArgumentNullException(nameof(deviceBusCommutation));
      _relayManager = new RelayManager(deviceBusCommutation);
    }

    /// <inheritdoc />
    public async Task<bool> ConnectRelay(int numberRelay, IUserInteractionService? userMessageService = null)
    {
      var result = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        var succes = await _relayManager.ConnectRelay(numberRelay);

        if (!succes || DeviceDisplayConfig.GetConnectionInfoVisibility())
        {
          await DeviceMessageBuilder.ShowConnectionMessageAsync(_deviceBusCommutation, "Подключение реле", $"№{numberRelay}", succes, 1, userMessageService);
        }

        return succes;
      }, userMessageService, deviceTask: true);

      if (!result)
        throw RelayControlExceptionFactory.ConnectFailed(numberRelay);

      return result;
    }

    /// <inheritdoc />
    public async Task<bool> DisconnectRelay(int numberRelay, IUserInteractionService? userMessageService = null)
    {
      var result = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        var succes = await _relayManager.DisconnectRelay(numberRelay);

        if (!succes || DeviceDisplayConfig.GetConnectionInfoVisibility())
        {
          await DeviceMessageBuilder.ShowConnectionMessageAsync(_deviceBusCommutation, "Отключение реле", $"№{numberRelay}", succes, 1, userMessageService);
        }

        return succes;
      }, userMessageService, deviceTask: true);

      if (!result)
        throw RelayControlExceptionFactory.DisconnectFailed(numberRelay);

      return result;
    }
  }
}
