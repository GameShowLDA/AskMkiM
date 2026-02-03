using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice.Capabilities;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using NewCore.Communication;

namespace NewCore.Function.DeviceBusCommutation
{
  /// <summary>
  /// Менеджер управления реле коммутации.
  /// Отвечает за подключение и отключение реле в системе.
  /// </summary>
  public class RelayManager : IRelayDeviceBusCommutation
  {
    /// <summary>
    /// Устройство коммутации шин.
    /// </summary>
    private readonly Device.DeviceBusCommutation _deviceBusCommutation;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="BusManager"/>.
    /// </summary>
    /// <param name="deviceBusCommutation">Экземпляр устройства коммутации шин.</param>
    public RelayManager(Device.DeviceBusCommutation deviceBusCommutation) => _deviceBusCommutation = deviceBusCommutation;

    /// <summary>
    /// Подключения реле.
    /// </summary>
    /// <param name="numberRelay">Номер реле, которое необходимо замкнуть.</param>
    /// <returns>Результат проверки и выполнения команды.</returns>
    public async Task<bool> ConnectRelay(int numberRelay, IUserInteractionService? userMessageService = null)
    {
      if (numberRelay < 0)
      {
        return false;
      }

      if (ExecutionConfig.GetIsIdleModeEnabled())
      {
        return true;
      }

      DeviceCommand cmd = new DeviceCommand(8, numberRelay, 1);
      await _deviceBusCommutation.DeviceProtocol.QueryAsync(cmd.ToString());
      await Task.Delay(10);
      return true;
    }

    /// <summary>
    /// Подключение реле.
    /// </summary>
    /// <param name="numberRelay">Номер реле, которое необходимо замкнуть.</param>
    /// <returns>Результат проверки и выполнения команды.</returns>
    public async Task<bool> DisconnectRelay(int numberRelay, IUserInteractionService? userMessageService = null)
    {
      if (numberRelay < 0)
      {
        return false;
      }

      if (ExecutionConfig.GetIsIdleModeEnabled())
      {
        return true;
      }

      DeviceCommand cmd = new DeviceCommand(8, numberRelay, 2);
      await _deviceBusCommutation.DeviceProtocol.QueryAsync(cmd.ToString());
      await Task.Delay(10);
      return true;
    }
  }
}
