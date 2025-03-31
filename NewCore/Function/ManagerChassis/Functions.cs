using NewCore.Communication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace NewCore.Function.ManagerChassis
{
  public class Functions
  {

    public Functions(Device.ManagerChassis managerChassis) => _chassisModel = managerChassis;

    Device.ManagerChassis _chassisModel;

    /// <summary>
    /// Запускает питание на АСК-МКИ-М.
    /// </summary>
    /// <returns> Возвращает объект типа Task.</returns>
    public async Task StartPowerAsync()
    {
      await DeviceCommandSender.SendCommandAsync(_chassisModel.IPAddress, new DeviceCommand(2, 1, 1));
    }

    /// <summary>
    /// Выключает питание на АСК-МКИ-М.
    /// </summary>
    /// <returns> Возвращает объект типа Task.</returns>
    public async Task StopPowerAsync()
    {
      await DeviceCommandSender.SendCommandAsync(_chassisModel.IPAddress, new DeviceCommand(2, 2, 1));
    }

    /// <summary>
    /// Инициализация устройства коммутации шин.
    /// </summary>
    /// <returns>Кортеж с булевым результатом и строкой, содержащей ответ от инициализации при ошибке.</returns>
    public async Task<(bool Connect, string Answer)> Initialize()
    {
      DeviceCommand cmd = new DeviceCommand(1, 0, 0, 0);
      string result = await DeviceCommandSender.SendCommandAsync(_chassisModel.IPAddress, cmd, 2000).ConfigureAwait(true);
      return result == "1.0.1" ? (true, string.Empty) : (false, result);
    }
  }
}
