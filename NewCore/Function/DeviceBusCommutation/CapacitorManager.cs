using NewCore.Base.Function.DBC;
using NewCore.Communication;
using NewCore.Device;
using static Utilities.LoggerUtility;

namespace NewCore.Function.DeviceBusCommutation
{
  /// <summary>
  /// Менеджер управления подлючением конденсаторов.
  /// Обеспечивает подключение и отключение конденсаторов в системе.
  /// </summary>
  public class CapacitorManager : ICapacitorDeviceBusCommutation
  {
    /// <summary>
    /// Устройство коммутации шин.
    /// </summary>
    private readonly Device.DeviceBusCommutation _deviceBusCommutation;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="BusManager"/>.
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
        await _deviceBusCommutation.DeviceProtocol.QueryAsync(command.ToString());
        // await DeviceCommandSender.SendCommandAsync(_deviceBusCommutation.IPAddress, command).ConfigureAwait(false);
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
        await _deviceBusCommutation.DeviceProtocol.QueryAsync(command.ToString());
        // await DeviceCommandSender.SendCommandAsync(_deviceBusCommutation.IPAddress, command).ConfigureAwait(false);
        return true;
      }

      LogError("Неверный номер конденсатора!");
      return false;
    }
  }
}
