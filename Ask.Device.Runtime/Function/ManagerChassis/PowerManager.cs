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
    private readonly ChassisQueryExecutor _queryExecutor;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="PowerManager"/>.
    /// </summary>
    /// <param name="managerChassis">Экземпляр менеджера шасси.</param>
    public PowerManager(IChassisManager managerChassis)
    {
      ChassisModel = managerChassis;
      _queryExecutor = new ChassisQueryExecutor(managerChassis);
    }

    /// <inheritdoc />
    public async Task StartPowerAsync(IUserInteractionService? userMessageService = null)
    {
      bool success = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        var cmd = new DeviceCommand(2, 1, 1);
        string response = await _queryExecutor.QueryAsync(cmd.ToString(), timeout: 0);
        if (ExecutionConfig.GetIsIdleModeEnabled())
        {
          bool result = string.Equals(response.Trim(), "1", StringComparison.Ordinal);
          await ShowIdleResultAsync("Включение питания шасси", result, userMessageService);
          return result;
        }

        return true;
      }, ExecutionConfig.GetIsIdleModeEnabled() ? userMessageService : null, deviceTask: true);

      ThrowIfFailed(success);
    }

    /// <inheritdoc />
    public async Task StopPowerAsync(IUserInteractionService? userMessageService = null)
    {
      bool success = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        var cmd = new DeviceCommand(2, 2, 1);
        string response = await _queryExecutor.QueryAsync(cmd.ToString(), timeout: 0);
        if (ExecutionConfig.GetIsIdleModeEnabled())
        {
          bool result = string.Equals(response.Trim(), "1", StringComparison.Ordinal);
          await ShowIdleResultAsync("Отключение питания шасси", result, userMessageService);
          return result;
        }

        return true;
      }, ExecutionConfig.GetIsIdleModeEnabled() ? userMessageService : null, deviceTask: true);

      ThrowIfFailed(success);
    }

    /// <inheritdoc />
    public async Task<bool> VerifyPowerAsync(IUserInteractionService? userMessageService = null)
    {
      return await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        var cmd = new DeviceCommand(7);
        string response = await _queryExecutor.QueryAsync(cmd.ToString(), timeout: 2000);
        bool result = response.Contains("1", StringComparison.Ordinal);
        await ShowIdleResultAsync("Проверка питания шасси", result, userMessageService);

        return result;
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
          message: success ? "Операция выполнена успешно." : string.Empty,
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
        throw new DeviceException("Оборудование не выполнило операцию.");
      }
    }
  }
}
