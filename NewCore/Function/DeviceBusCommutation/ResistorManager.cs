using NewCore.Communication;
using static Utilities.LoggerUtility;

namespace NewCore.Function.DeviceBusCommutation
{
  public class ResistorManager
  {
    /// <summary>
    /// Устройство коммутации шин.
    /// </summary>
    private readonly Device.DeviceBusCommutation _deviceBusCommutation;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="ResistorManager"/>.
    /// </summary>
    /// <param name="deviceBusCommutation">Экземпляр устройства коммутации шин.</param>
    public ResistorManager(Device.DeviceBusCommutation deviceBusCommutation) => _deviceBusCommutation = deviceBusCommutation;

    /// <summary>
    /// Замыкание резистора.
    /// </summary>
    /// <param name="number">Номер резистора.</param>
    /// <returns>Задача (Task), представляющая асинхронную операцию.</returns>
    public async Task<bool> ConnectResistor(string number)
    {
      if (int.TryParse(number, out int num))
      {
        DeviceCommand command = new DeviceCommand(6, 1, num, 1);
        await DeviceCommandSender.SendCommandAsync(_deviceBusCommutation.IPAddress, command).ConfigureAwait(false);
        return true;
      }

      LogError("Неверный номер резистора!");
      return false;
    }

    /// <summary>
    /// Размыкание резистора.
    /// </summary>
    /// <param name="number">Номер резистора.</param>
    /// <returns>Задача (Task), представляющая асинхронную операцию.</returns>
    public async Task<bool> DisconnectResistor(string number)
    {
      if (int.TryParse(number, out int num))
      {
        DeviceCommand command = new DeviceCommand(6, 1, num, 2);
        await DeviceCommandSender.SendCommandAsync(_deviceBusCommutation.IPAddress, command).ConfigureAwait(false);
        return true;
      }

      LogError("Неверный номер резистора!");
      return false;
    }
  }
}
