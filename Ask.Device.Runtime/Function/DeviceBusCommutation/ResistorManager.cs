using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice.Capabilities;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Device.Communication.Ethernet.Udp;
using Ask.Device.Communication.Ethernet.Udp.Commands;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Device.Runtime.Function.DeviceBusCommutation
{
  /// <summary>
  /// Менеджер управления коммутацией резисторов.
  /// Обеспечивает подключение и отключение резисторов в системе.
  /// </summary>
  public class ResistorManager : IResistorDeviceBusCommutation
  {
    /// <summary>
    /// Устройство коммутации шин.
    /// </summary>
    private readonly Device.DeviceBusCommutation _deviceBusCommutation;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="BusManager"/>.
    /// </summary>
    /// <param name="deviceBusCommutation">Экземпляр устройства коммутации шин.</param>
    public ResistorManager(Device.DeviceBusCommutation deviceBusCommutation) => _deviceBusCommutation = deviceBusCommutation;

    /// <summary>
    /// Замыкание резистора.
    /// </summary>
    /// <param name="number">Номер резистора.</param>
    /// <returns>Задача (Task), представляющая асинхронную операцию.</returns>
    public async Task<bool> ConnectResistor(string number, IUserInteractionService? userMessageService = null)
    {
      if (int.TryParse(number, out int num))
      {
        if (ExecutionConfig.GetIsIdleModeEnabled())
        {
          return true;
        }

        DeviceCommand cmd = new DeviceCommand(6, 1, num, 1);
        await _deviceBusCommutation.DeviceProtocol.QueryAsync(cmd.ToString());

        return true;
      }

      LogError("Неверный номер резистора!", isDeviceLog: true);
      return false;
    }

    /// <summary>
    /// Размыкание резистора.
    /// </summary>
    /// <param name="number">Номер резистора.</param>
    /// <returns>Задача (Task), представляющая асинхронную операцию.</returns>
    public async Task<bool> DisconnectResistor(string number, IUserInteractionService? userMessageService = null)
    {
      if (int.TryParse(number, out int num))
      {
        if (ExecutionConfig.GetIsIdleModeEnabled())
        {
          return true;
        }

        DeviceCommand cmd = new DeviceCommand(6, 1, num, 2);
        await _deviceBusCommutation.DeviceProtocol.QueryAsync(cmd.ToString());

        return true;
      }

      LogError("Неверный номер резистора!", isDeviceLog: true);
      return false;
    }
  }
}
