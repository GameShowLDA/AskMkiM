using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Errors.Device;
using Ask.Core.Services.UI;
using Ask.Core.Shared.DTO.Protocol;
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
      bool success = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        if (ExecutionConfig.GetIsIdleModeEnabled())
        {
          bool result = !IdleHardwareErrorSimulator.ShouldSimulateHardwareError();
          await ShowIdleResultAsync("Включение питания шасси", result, userMessageService);
          return result;
        }

        var cmd = new DeviceCommand(2, 1, 1);
        await ChassisModel.DeviceProtocol.QueryAsync(cmd.ToString());
        return true;
      }, ExecutionConfig.GetIsIdleModeEnabled() ? userMessageService : null, deviceTask: true);

      ThrowIfFailed(success);
    }

    /// <inheritdoc />
    public async Task StopPowerAsync(IUserInteractionService? userMessageService = null)
    {
      bool success = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        if (ExecutionConfig.GetIsIdleModeEnabled())
        {
          bool result = !IdleHardwareErrorSimulator.ShouldSimulateHardwareError();
          await ShowIdleResultAsync("Отключение питания шасси", result, userMessageService);
          return result;
        }

        var cmd = new DeviceCommand(2, 2, 1);
        await ChassisModel.DeviceProtocol.QueryAsync(cmd.ToString());
        return true;
      }, ExecutionConfig.GetIsIdleModeEnabled() ? userMessageService : null, deviceTask: true);

      ThrowIfFailed(success);
    }

    /// <inheritdoc />
    public async Task<bool> VerifyPowerAsync(IUserInteractionService? userMessageService = null)
    {
      return await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        if (ExecutionConfig.GetIsIdleModeEnabled())
        {
          bool attemptSuccess = !IdleHardwareErrorSimulator.ShouldSimulateHardwareError();
          await ShowIdleResultAsync("Проверка питания шасси", attemptSuccess, userMessageService);
          return attemptSuccess;
        }

        var cmd = new DeviceCommand(7);
        var result = await ChassisModel.DeviceProtocol.QueryAsync(cmd.ToString(), timeout: 2000);

        return result.Contains("1");
      }, ExecutionConfig.GetIsIdleModeEnabled() ? userMessageService : null, deviceTask: true);
    }

    /// <summary>
    /// Отображает результат аппаратной операции холостого режима.
    /// </summary>
    /// <param name="operationName">Название аппаратной операции.</param>
    /// <param name="success">Результат аппаратной операции.</param>
    /// <param name="userMessageService">Сервис взаимодействия с пользователем.</param>
    private async Task ShowIdleResultAsync(
      string operationName,
      bool success,
      IUserInteractionService? userMessageService)
    {
      if (!ExecutionConfig.GetIsIdleModeEnabled() || userMessageService == null)
      {
        return;
      }

      await userMessageService.ShowMessageAsync(
        new ShowMessageModel(
          header: $"{ChassisModel.Name} - {operationName}",
          message: success ? "Операция выполнена успешно." : IdleHardwareErrorSimulator.ErrorMessage,
          type: success
            ? ShowMessageModel.MessageType.Success
            : ShowMessageModel.MessageType.Error),
        skipPause: true);
    }

    /// <summary>
    /// Проверяет результат операции управления питанием.
    /// </summary>
    /// <param name="success">Результат операции управления питанием.</param>
    /// <exception cref="DeviceException">
    /// Выбрасывается, если операция завершилась ошибкой.
    /// </exception>
    private static void ThrowIfFailed(bool success)
    {
      if (!success)
      {
        throw new DeviceException(IdleHardwareErrorSimulator.ErrorMessage);
      }
    }
  }
}
