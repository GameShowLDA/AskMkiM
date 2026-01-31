using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using NewCore.Communication;

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

    public event Action DeviceDisponce;
    public event Action IsReset;

    /// <summary>
    /// Создаёт новый экземпляр класса <see cref="StateManager"/>.
    /// </summary>
    /// <param name="managerChassis">Экземпляр менеджера шасси.</param>
    public StateManager(Device.ManagerChassis managerChassis) => _chassisModel = managerChassis;

    /// <inheritdoc />
    public async Task<(bool Connect, string Answer)> InitializeAsync(IUserInteractionService messageService = null)
    {
      if (ExecutionConfig.GetIsIdleModeEnabled())
      {
        return (true, "Включен холостой режим");
      }

      DeviceCommand cmd = new DeviceCommand(1, 0, 0, 0);
      string result = await _chassisModel.DeviceProtocol.QueryAsync(cmd.ToString(), timeout: 2000);
      return result == "1.0.1" ? (true, string.Empty) : (false, result);
    }

    /// <inheritdoc />
    public async Task<bool> ResetAsync(IUserInteractionService messageService = null)
    {
      if (ExecutionConfig.GetIsIdleModeEnabled())
      {
        return true;
      }

      DeviceCommand cmd = new DeviceCommand(2, 0, 0, 0);
      string result = await _chassisModel.DeviceProtocol.QueryAsync(cmd.ToString(), timeout: 1000);
      IsReset?.Invoke();
      return result == "2.0.1";
    }

    /// <inheritdoc />
    public async Task<(bool Connect, string Answer)> ConnectAsync(IUserInteractionService messageService = null)
    {
      return await InitializeAsync();
    }

    /// <inheritdoc />
    public async Task<bool> DisconnectAsync(IUserInteractionService messageService = null)
    {
      return await ResetAsync();
    }

    public string GetConnectionStatus()
    {
      throw new NotImplementedException();
    }
  }
}
