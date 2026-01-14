using Ask.Core.Services.Errors.Device.DeviceBusCommutation;
using Ask.Core.Services.UI;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice.Capabilities;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using NewCore.Function.DeviceBusCommutation;
using NewCore.Function.Helpers;

namespace NewCore.FunctionAdapters.DeviceBusCommutation
{
  internal class CapacitorManagerAdapter : ICapacitorDeviceBusCommutation
  {
    /// <summary>
    /// Устройство коммутации шин.
    /// </summary>
    private readonly Device.DeviceBusCommutation _deviceBusCommutation;

    private readonly CapacitorManager _capacitorManager;

    private Dictionary<int, bool> IsConnectCapacitor = new Dictionary<int, bool>();

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="BusManager"/>.
    /// </summary>
    /// <param name="deviceBusCommutation">Экземпляр устройства коммутации шин.</param>
    public CapacitorManagerAdapter(Device.DeviceBusCommutation deviceBusCommutation)
    {
      _deviceBusCommutation = deviceBusCommutation ?? throw new ArgumentNullException(nameof(deviceBusCommutation));
      _capacitorManager = new CapacitorManager(deviceBusCommutation);
      _deviceBusCommutation.ConnectableManager.IsReset += ConnectableManager_IsReset;
    }

    private void ConnectableManager_IsReset()
    {
      IsConnectCapacitor.Clear();
    }

    /// <inheritdoc />
    public async Task<bool> ConnectCapacitor(int number, IUserInteractionService? userMessageService = null)
    {
      IsConnectCapacitor.TryGetValue(number, out bool connect);
      if (connect)
      {
        return true;
      }

      var result = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        var succes = await _capacitorManager.ConnectCapacitor(number);
        await DeviceMessageBuilder.ShowConnectionMessageAsync(_deviceBusCommutation, "Подключение конденсатора", number.ToString(), succes, 1, userMessageService);
        return succes;
      }, userMessageService, deviceTask: true);


      if (!result)
      {
        throw CapacitorExceptionFactory.ConnectFailed(number.ToString());
      }
      else
      {
        IsConnectCapacitor[number] = true;
      }
      return result;
    }

    /// <inheritdoc />
    public async Task<bool> DisconnectCapacitor(int number, IUserInteractionService? userMessageService = null)
    {
      IsConnectCapacitor.TryGetValue(number, out bool connect);
      if (!connect)
      {
        return true;
      }

      var result = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        var succes = await _capacitorManager.DisconnectCapacitor(number);
        await DeviceMessageBuilder.ShowConnectionMessageAsync(_deviceBusCommutation, "Отключение конденсатора", number.ToString(), succes, 1, userMessageService);

        return succes;
      }, userMessageService, deviceTask: true);


      if (!result)
      {
        throw CapacitorExceptionFactory.DisconnectFailed(number.ToString());
      }
      else
      {
        IsConnectCapacitor[number] = true;
      }

      return result;
    }
  }
}
