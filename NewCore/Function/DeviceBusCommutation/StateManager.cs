using NewCore.Communication;
using NewCore.Device;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewCore.Function.DeviceBusCommutation
{
  public class StateManager
  {
    /// <summary>
    /// Устройство коммутации шин.
    /// </summary>
    private readonly Device.DeviceBusCommutation _deviceBusCommutation;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="StateManager"/>.
    /// </summary>
    /// <param name="deviceBusCommutation">Экземпляр устройства коммутации шин.</param>
    public StateManager(Device.DeviceBusCommutation deviceBusCommutation) => _deviceBusCommutation = deviceBusCommutation;

    /// <summary>
    /// Сброс всех реле на УКШ.
    /// </summary>
    /// <param name="_deviceBusCommutation.IPAddress">"Ip адрес УКШ.".</param>
    /// <returns>Задача (Task), представляющая асинхронную операцию.</returns>
    public async Task<bool> ResetAsync()
    {
      DeviceCommand cmd = new DeviceCommand(2, 0, 0, 0);
      string result = await DeviceCommandSender.SendCommandAsync(_deviceBusCommutation.IPAddress, cmd, 1000).ConfigureAwait(true);
      return result == "2.0.1";
    }

    /// <summary>
    /// Инициализация устройства коммутации шин.
    /// </summary>
    /// <param name="_deviceBusCommutation.IPAddress">"Ip адрес УКШ.".</param>
    /// <returns>Возвращает ответ, получен ли ответ от инициализации.</returns>
    public async Task<(bool Connect, string Answer)> Initialize()
    {
      DeviceCommand cmd = new DeviceCommand(1, 0, 0, 0);
      string result = await DeviceCommandSender.SendCommandAsync(_deviceBusCommutation.IPAddress, cmd, 2000).ConfigureAwait(true);

      // TODO : Тут ошибка, я не знаю ответ от УКШ...
      return result == "1.0.1" ? (true, string.Empty) : (false, result);
    }
  }
}
