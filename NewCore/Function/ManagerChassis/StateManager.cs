using System.Net;
using NewCore.Base.Function.ManagerChassis;
using NewCore.Base.Interface.Main;
using NewCore.Communication;

namespace NewCore.Function.ManagerChassis
{
  /// <summary>
  /// Класс для управления состоянием шасси.
  /// </summary>
  public class StateManager : IStateManagerChassis
  {
    /// <summary>
    /// Экземпляр менеджера шасси.
    /// </summary>
    private readonly IChassisManager _chassisModel;

    /// <summary>
    /// Создаёт новый экземпляр класса <see cref="StateManager"/>.
    /// </summary>
    /// <param name="managerChassis">Экземпляр менеджера шасси.</param>
    public StateManager(IChassisManager managerChassis) => _chassisModel = managerChassis;

    /// <summary>
    /// Инициализирует устройство коммутации шин.
    /// </summary>
    /// <returns>
    /// Кортеж, содержащий:
    /// <list type="bullet">
    /// <item><c>bool Connect</c> - результат инициализации (успешно/неуспешно).</item>
    /// <item><c>string Answer</c> - ответ от устройства при ошибке.</item>
    /// </list>
    /// </returns>
    public async Task<(bool Connect, string Answer)> Initialize()
    {
      DeviceCommand cmd = new DeviceCommand(1, 0, 0, 0);
      string result = await DeviceCommandSender.SendCommandAsync(IPAddress.Parse(_chassisModel.ConnectionDetails), cmd, 2000).ConfigureAwait(true);
      return result == "1.0.1" ? (true, string.Empty) : (false, result);
    }
  }
}
