using System.Net;
using NewCore.Base.Function.ManagerChassis;
using NewCore.Base.Interface.Main;
using NewCore.Communication;

namespace NewCore.Function.ManagerChassis
{
  public class StateManager : IStateManagerChassis
  {
    IChassisManager _chassisModel;
    public StateManager(IChassisManager managerChassis) => _chassisModel = managerChassis;

    /// <summary>
    /// Инициализация устройства коммутации шин.
    /// </summary>
    /// <returns>Кортеж с булевым результатом и строкой, содержащей ответ от инициализации при ошибке.</returns>
    public async Task<(bool Connect, string Answer)> Initialize()
    {
      DeviceCommand cmd = new DeviceCommand(1, 0, 0, 0);
      string result = await DeviceCommandSender.SendCommandAsync(IPAddress.Parse(_chassisModel.ConnectionDetails), cmd, 2000).ConfigureAwait(true);
      return result == "1.0.1" ? (true, string.Empty) : (false, result);
    }
  }
}
