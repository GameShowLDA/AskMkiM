using NewCore.Communication;
using static Utilities.LoggerUtility;


namespace NewCore.Function.DeviceBusCommutation
{
  public class CapacitorManager
  {
    /// <summary>
    /// Устройство коммутации шин.
    /// </summary>
    private readonly Device.DeviceBusCommutation _deviceBusCommutation;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="CapacitorManager"/>.
    /// </summary>
    /// <param name="deviceBusCommutation">Экземпляр устройства коммутации шин.</param>
    public CapacitorManager(Device.DeviceBusCommutation deviceBusCommutation) => _deviceBusCommutation = deviceBusCommutation;

    /// <summary>
    /// Замыкание конденсатора.
    /// </summary>
    /// <param name="number">Номер конденсатора.</param>
    /// <returns>Задача (Task), представляющая асинхронную операцию.</returns>
    public async Task<bool> ConnectCapacitor(string number)
    {
      if (int.TryParse(number, out int num))
      {
        DeviceCommand command = new DeviceCommand(6, 2, num, 1);
        await DeviceCommandSender.SendCommandAsync(_deviceBusCommutation.IPAddress, command).ConfigureAwait(false);
        return true;
      }

      LogError("Неверный номер конденсатора!");
      return false;
    }

    /// <summary>
    /// Размыкание конденсатора.
    /// </summary>
    /// <param name="number">Номер конденсатора.</param>
    /// <returns>Задача (Task), представляющая асинхронную операцию.</returns>
    public async Task<bool> DisconnectCapacitor(string number)
    {
      if (int.TryParse(number, out int num))
      {
        DeviceCommand command = new DeviceCommand(6, 2, num, 2);
        await DeviceCommandSender.SendCommandAsync(_deviceBusCommutation.IPAddress, command).ConfigureAwait(false);
        return true;
      }

      LogError("Неверный номер конденсатора!");
      return false;
    }
  }
}
