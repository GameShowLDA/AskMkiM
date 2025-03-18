using System.Net;
using NewCore.Base.Function.ManagerChassis;
using NewCore.Base.Interface.Main;
using NewCore.Communication;

namespace NewCore.Function.ManagerChassis
{
  public class PowerManager : IPowerManagerChassis
  {
    IChassisManager _chassisModel;
    public PowerManager(IChassisManager managerChassis) => _chassisModel = managerChassis;

    /// <summary>
    /// Запускает питание на АСК-МКИ-М.
    /// </summary>
    /// <returns> Возвращает объект типа Task.</returns>
    public async Task StartPowerAsync()
    {
      await DeviceCommandSender.SendCommandAsync(IPAddress.Parse(_chassisModel.ConnectionDetails), new DeviceCommand(2, 1, 1));
    }

    /// <summary>
    /// Выключает питание на АСК-МКИ-М.
    /// </summary>
    /// <returns> Возвращает объект типа Task.</returns>
    public async Task StopPowerAsync()
    {
      await DeviceCommandSender.SendCommandAsync(IPAddress.Parse(_chassisModel.ConnectionDetails), new DeviceCommand(2, 2, 1));
    }
  }
}
