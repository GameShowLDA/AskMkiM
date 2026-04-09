using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Config.Base;
using Ask.Core.Shared.DTO.Protocol;
using Ask.DataBase.Engine.Initialization;
using Ask.DataBase.Engine.Static;
using Ask.DataBase.Engine.Static.Devices;
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
          $"New DB initialized. Applied migrations: {newDatabaseReport.AppliedMigrations}, " +
          $"used EnsureCreated: {newDatabaseReport.UsedEnsureCreated}, " +
          $"seeded hotkeys: {newDatabaseReport.SeededHotkeys}, " +
          $"created default settings rows: {newDatabaseReport.CreatedDefaultSettingsRows}.");

        await WarmUpDeviceCachesAsync();

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

        ProtocolConfig.SaveProtocolAsyncEvent += async model =>
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
    /// Warms up the device cache for all device services at application startup.
    /// </summary>
    private static async Task WarmUpDeviceCachesAsync()
    {
      try
      {
        DeviceRuntime.ClearCache();

        var chassisTask = ChassisManagers.GetAllAsync();
        var racksTask = Racks.GetAllAsync();
        var fastMetersTask = FastMeters.GetAllAsync();
        var breakdownTask = BreakdownTesters.GetAllAsync();
        var powerSourcesTask = PowerSourceModules.GetAllAsync();
        var relaySwitchModulesTask = RelaySwitchModules.GetAllAsync();
        var switchingDevicesTask = SwitchingDevices.GetAllAsync();
        var uninterruptiblePowerSuppliesTask = UninterruptiblePowerSupplies.GetAllAsync();

        await Task.WhenAll(
          chassisTask,
          racksTask,
          fastMetersTask,
          breakdownTask,
          powerSourcesTask,
          relaySwitchModulesTask,
          switchingDevicesTask,
          uninterruptiblePowerSuppliesTask);

        int totalDevices =
          chassisTask.Result.Count +
          racksTask.Result.Count +
          fastMetersTask.Result.Count +
          breakdownTask.Result.Count +
          powerSourcesTask.Result.Count +
          relaySwitchModulesTask.Result.Count +
          switchingDevicesTask.Result.Count +
          uninterruptiblePowerSuppliesTask.Result.Count;

        LogInformation($"Device cache warmed up on startup. Loaded devices: {totalDevices}.");
      }
      catch (Exception ex)
      {
        LogException(ex, "Device cache warm-up failed.");
      }
    }
  }
}
