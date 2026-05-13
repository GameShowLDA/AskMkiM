using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Capabilities;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Chassis;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Device.Runtime.Commands;

namespace Ask.Device.Runtime.Function.ManagerChassis
{
  /// <summary>
  /// Класс для управления питанием шасси.
  /// </summary>
  public class PowerManager : IPower
  {
    /// <summary>
    /// Интерфейс управления шасси.
    /// </summary>
    private IChassisManager ChassisModel { get; set; }

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="PowerManager"/>.
    /// </summary>
    /// <param name="managerChassis">Экземпляр менеджера шасси.</param>
    public PowerManager(IChassisManager managerChassis) => ChassisModel = managerChassis;

    /// <inheritdoc />
    public async Task StartPowerAsync(IUserInteractionService? userMessageService = null)
    {
      if (ExecutionConfig.GetIsIdleModeEnabled())
      {
        return;
      }

      var cmd = new DeviceCommand(2, 1, 1);
      await ChassisModel.DeviceProtocol.QueryAsync(cmd.ToString());
    }

    /// <inheritdoc />
    public async Task StopPowerAsync(IUserInteractionService? userMessageService = null)
    {
      if (ExecutionConfig.GetIsIdleModeEnabled())
      {
        return;
      }

      var cmd = new DeviceCommand(2, 2, 1);
      await ChassisModel.DeviceProtocol.QueryAsync(cmd.ToString());
    }

    /// <inheritdoc />
    public async Task<bool> VerifyPowerAsync(IUserInteractionService? userMessageService = null)
    {
      if (ExecutionConfig.GetIsIdleModeEnabled())
      {
        return true;
      }

      var cmd = new DeviceCommand(7);
      var result = await ChassisModel.DeviceProtocol.QueryAsync(cmd.ToString(), timeout: 2000);

      return result.Contains("1");
    }
  }
}
