using System.Net;
using NewCore.Base.Device;
using NewCore.Base.Function.ManagerChassis;
using NewCore.Base.Interface.Main;
using NewCore.Communication;
using NewCore.Device;

namespace NewCore.Function.ManagerChassis
{
  /// <summary>
  /// Класс для управления состоянием шасси.
  /// </summary>
  public class StateManager : IConnectable
  {
    /// <summary>
    /// Экземпляр менеджера шасси.
    /// </summary>
    private readonly Device.ManagerChassis _chassisModel;

    /// <summary>
    /// Создаёт новый экземпляр класса <see cref="StateManager"/>.
    /// </summary>
    /// <param name="managerChassis">Экземпляр менеджера шасси.</param>
    public StateManager(Device.ManagerChassis managerChassis) => _chassisModel = managerChassis;

    /// <inheritdoc />
    public async Task<(bool Connect, string Answer)> InitializeAsync()
    {
      DeviceCommand cmd = new DeviceCommand(1, 0, 0, 0);
      string result = await _chassisModel.DeviceProtocol.QueryAsync(cmd.ToString(), 2000);
      return result == "1.0.1" ? (true, string.Empty) : (false, result);
    }

    /// <inheritdoc />
    public async Task<bool> ResetAsync()
    {
      DeviceCommand cmd = new DeviceCommand(2, 0, 0, 0);
      string result = await _chassisModel.DeviceProtocol.QueryAsync(cmd.ToString(), 1000);
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
