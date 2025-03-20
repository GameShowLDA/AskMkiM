using System;
using System.Net;
using System.Threading.Tasks;
using NewCore.Base.Function.ModuleRelayControl;
using NewCore.Base.Interface.Main;
using NewCore.Communication;
using NewCore.Device;

namespace NewCore.Function.ModuleRelayControl
{
  /// <summary>
  /// Управляет состоянием модуля коммутации реле (МКР).
  /// </summary>
  public class StateManager : IStateManager
  {
    /// <summary>
    /// Экземпляр интерфейса модуля реле.
    /// </summary>
    private readonly IRelaySwitchModule _moduleRelayControl;

    /// <summary>
    /// Создаёт новый экземпляр класса <see cref="StateManager"/>.
    /// </summary>
    /// <param name="moduleRelayControl">Экземпляр интерфейса модуля реле.</param>
    public StateManager(IRelaySwitchModule moduleRelayControl) => _moduleRelayControl = moduleRelayControl;

    /// <summary>
    /// Инициализирует модуль коммутации реле.
    /// </summary>
    /// <returns>
    /// Кортеж, содержащий булево значение (<c>true</c>, если инициализация успешна) 
    /// и строку с ответом устройства в случае ошибки.
    /// </returns>
    public async Task<(bool Connect, string Answer)> Initialize()
    {
      DeviceCommand cmd = new DeviceCommand(1, 0, 0, 0);
      string result = await DeviceCommandSender.SendCommandAsync(IPAddress.Parse(_moduleRelayControl.ConnectionDetails), cmd, 2000).ConfigureAwait(true);
      return result == "1.0.1" ? (true, string.Empty) : (false, result);
    }

    /// <summary>
    /// Выполняет сброс всех реле на модуле коммутации реле (МКР).
    /// </summary>
    /// <returns>Асинхронная задача, представляющая операцию сброса реле.</returns>
    public async Task ResetAsync()
    {
      await DeviceCommandSender.SendCommandAsync(IPAddress.Parse(_moduleRelayControl.ConnectionDetails), new DeviceCommand(2));
    }
  }
}
