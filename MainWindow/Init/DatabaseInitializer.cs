using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Config.Base;
using Ask.Core.Shared.DTO.Protocol;
using Ask.DataBase.Engine.Initialization;
using Ask.DataBase.Engine.Static;
using Ask.DataBase.Engine.Static.Settings;
using static Ask.LogLib.LoggerUtility;

namespace MainWindowProgram.Init
{
  static internal class DatabaseInitializer
  {
    static internal async Task InitializeAsync()
    {
      try
      {
        var newDatabaseReport = await DatabaseEngineInitializer.InitializeAsync();
        LogInformation(
          $"Новая БД инициализирована. Применено миграций: {newDatabaseReport.AppliedMigrations}, " +
          $"использован EnsureCreated: {newDatabaseReport.UsedEnsureCreated}, " +
          $"добавлено горячих клавиш: {newDatabaseReport.SeededHotkeys}, " +
          $"создано строк настроек: {newDatabaseReport.CreatedDefaultSettingsRows}.");
        WarmUpDeviceCaches();

        var protocolTask = ProtocolSettings.GetAsync();
        var executionTask = ExecutionSettings.GetAsync();
        var userInterfaceTask = UserInterfaceSettings.GetAsync();
        var deviceDisplayTask = DeviceDisplaySettings.GetAsync();

        Task.WaitAll(protocolTask, executionTask, userInterfaceTask, deviceDisplayTask);

        var protocol = protocolTask.Result;
        var execution = executionTask.Result;
        var userInterface = userInterfaceTask.Result;
        var deviceDisplay = deviceDisplayTask.Result;

        if (protocol != null)
        {
          ProtocolConfig.SetProtocolModel(protocol);
          ProtocolModel.SetTemplate(protocol.CleanTextProtocol);
          ProtocolModel.SetErrorsTemplate(protocol.CleanTextErrorsProtocol);
        }

        if (execution != null)
        {
          await ExecutionConfig.SetExecutionModel(execution);
        }

        if (userInterface != null)
        {
          await UserInterfaceConfig.SetUserInterfaceModel(userInterface);
        }

        if (deviceDisplay != null)
        {
          await DeviceDisplayConfig.SetDeviceDisplaySettingsModel(deviceDisplay);
        }

        ProtocolConfig.SaveProtocolEvent += async model =>
        {
          await ProtocolSettings.SaveAsync(model);
          ProtocolModel.SetTemplate(model.CleanTextProtocol);
          ProtocolModel.SetErrorsTemplate(model.CleanTextErrorsProtocol);
        };

        ExecutionConfig.SaveExecutionEvent += async model =>
        {
          await ExecutionSettings.SaveAsync(model);
        };

        UserInterfaceConfig.SaveUserInterfaceAsyncEvent += async model =>
        {
          await UserInterfaceSettings.SaveAsync(model);
        };

        DeviceDisplayConfig.DeviceDisplaySettingsSaved += async model =>
        {
          await DeviceDisplaySettings.SaveAsync(model);
        };
      }
      catch (Exception ex)
      {
        LogException(ex);
      }
    }

    /// <summary>
    /// Прогревает кэш всех сервисов устройств при старте приложения.
    /// </summary>
    private static void WarmUpDeviceCaches()
    {
      try
      {
        DeviceRuntime.ClearCache();

        LogInformation("Кэш устройств прогрет при старте приложения.");
      }
      catch (Exception ex)
      {
        LogException(ex, "Ошибка прогрева кэша устройств.");
      }
    }
  }
}
