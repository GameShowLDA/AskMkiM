using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using NewCore.Base.Function.ModuleRelayControl;
using NewCore.Base.Interface.Main;
using NewCore.Communication;
using NewCore.Device;

namespace NewCore.Function.ModuleRelayControl
{
  public class StateManager : IStateManager
  {
    IRelaySwitchModule _moduleRelayControl { get; set; }
    public StateManager(IRelaySwitchModule moduleRelayControl) => _moduleRelayControl = moduleRelayControl;

    /// <summary>
    /// Инициализация модуля коммутации реле.
    /// </summary>
    /// <returns>Возвращает ответ, получен ли ответ от инициализации.</returns>
    public async Task<(bool Connect, string Answer)> Initialize()
    {
      DeviceCommand cmd = new DeviceCommand(1, 0, 0, 0);
      string result = await DeviceCommandSender.SendCommandAsync(IPAddress.Parse(_moduleRelayControl.ConnectionDetails), cmd, 2000).ConfigureAwait(true);
      return result == "1.0.1" ? (true, string.Empty) : (false, result);
    }

    /// <summary>
    /// Выполняет сброс всех реле на МКР.
    /// </summary>
    /// <param name="_moduleRelayControl.IPAddress">IP адресc.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task ResetAsync()
    {
      await DeviceCommandSender.SendCommandAsync(IPAddress.Parse(_moduleRelayControl.ConnectionDetails), new DeviceCommand(2));
    }

  }
}
