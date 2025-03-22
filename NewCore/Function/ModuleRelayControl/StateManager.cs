using System.Net;
using NewCore.Base.Device;
using NewCore.Base.Function.ModuleRelayControl;
using NewCore.Base.Interface.Main;
using NewCore.Communication;
using NewCore.Device;

namespace NewCore.Function.ModuleRelayControl
{
  /// <summary>
  /// Управляет состоянием модуля коммутации реле (МКР).
  /// </summary>
  public class StateManager : IConnectable
  {
    /// <summary>
    /// Экземпляр интерфейса модуля реле.
    /// </summary>
    private readonly Device.ModuleRelayControl _moduleRelayControl;

    /// <summary>
    /// Создаёт новый экземпляр класса <see cref="StateManager"/>.
    /// </summary>
    /// <param name="moduleRelayControl">Экземпляр интерфейса модуля реле.</param>
    public StateManager(Device.ModuleRelayControl moduleRelayControl) => _moduleRelayControl = moduleRelayControl;

    /// <inheritdoc />
    public async Task<(bool Connect, string Answer)> InitializeAsync()
    {
      DeviceCommand cmd = new DeviceCommand(1, 0, 0, 0);
      string result = await _moduleRelayControl.DeviceProtocol.QueryAsync(cmd.ToString(), 2000);
      return result == "1.0.1" ? (true, string.Empty) : (false, result);
    }

    /// <inheritdoc />
    public async Task<bool> ResetAsync()
    {
      DeviceCommand cmd = new DeviceCommand(2, 0, 0, 0);
      string result = await _moduleRelayControl.DeviceProtocol.QueryAsync(cmd.ToString(), 1000);
      return result == "2.0.1";
    }

    /// <inheritdoc />
    public async Task<(bool Connect, string Answer)> ConnectAsync()
    {
      return await InitializeAsync();
    }

    /// <inheritdoc />
    public async Task<bool> DisconnectAsync()
    {
      return await ResetAsync();
    }
  }
}
