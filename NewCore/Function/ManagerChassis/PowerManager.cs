using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Chassis;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Chassis.Capabilities;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using NewCore.Communication;

namespace NewCore.Function.ManagerChassis
{
  /// <summary>
  /// Класс для управления питанием шасси.
  /// </summary>
  public class PowerManager : IPowerManagerChassis
  {
    /// <summary>
    /// Интерфейс управления шасси.
    /// </summary>
    private IChassisManager _chassisModel { get; set; }

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="PowerManager"/>.
    /// </summary>
    /// <param name="managerChassis">Экземпляр менеджера шасси.</param>
    public PowerManager(IChassisManager managerChassis) => _chassisModel = managerChassis;

    /// <inheritdoc />
    public async Task StartPowerAsync(IUserInteractionService? userMessageService = null)
    {
      if (await ExecutionConfig.GetIsIdleModeEnabled())
      {
        return;
      }

      var cmd = new DeviceCommand(2, 1, 1);
      await _chassisModel.DeviceProtocol.QueryAsync(cmd.ToString());
    }

    /// <inheritdoc />
    public async Task StopPowerAsync(IUserInteractionService? userMessageService = null)
    {
      if (await ExecutionConfig.GetIsIdleModeEnabled())
      {
        return;
      }

      var cmd = new DeviceCommand(2, 2, 1);
      await _chassisModel.DeviceProtocol.QueryAsync(cmd.ToString());
    }
  }
}
