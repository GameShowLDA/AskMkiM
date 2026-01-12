using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Config.Base;
using Ask.Core.Shared.DTO.Protocol;
using DataBaseConfiguration;
using static Ask.LogLib.LoggerUtility;

namespace MainWindowProgram.Init
{
  static internal class DatabaseInitializer
  {
    static internal async Task InitializeAsync()
    {
      try
      {
        await DataBaseConfig.InitializeDB();

        var protocolTask = new DataBaseConfiguration.Services.Settings.ProtocolService().GetProtocolAsync();
        var executionTask = new DataBaseConfiguration.Services.Settings.ExecutionService().GetExecutionAsync();
        var userInterfaceTask = new DataBaseConfiguration.Services.Settings.UserInterfaceService().GetUserInterfaceAsync();
        var deviceDisplayTask = new DataBaseConfiguration.Services.Settings.DeviceDisplayService().GetDeviceDisplayAsync();

        Task.WaitAll(protocolTask, executionTask, userInterfaceTask, deviceDisplayTask);

        var protocol = protocolTask.Result;
        var execution = executionTask.Result;
        var userInreface = userInterfaceTask.Result;
        var deviceDisplay = deviceDisplayTask.Result;

        if (protocol != null)
        {
          await ProtocolConfig.SetProtocolModel(protocol);
          ProtocolModel.SetTemplate(protocol.CleanTextProtocol);
          ProtocolModel.SetErrorsTemplate(protocol.CleanTextErrorsProtocol);
        }

        if (execution != null)
        {
          await ExecutionConfig.SetExecutionModel(execution);
        }

        if (userInreface != null)
        {
          await UserInterfaceConfig.SetUserInterfaceModel(userInreface);
        }

        if (deviceDisplay != null)
        {
          await DeviceDisplayConfig.SetDeviceDisplaySettingsModel(deviceDisplay);
        }

        ProtocolConfig.SaveProtocolEvent += async (model) =>
        {
          var service = new DataBaseConfiguration.Services.Settings.ProtocolService();
          await service.SaveProtocolAsync(model);
          ProtocolModel.SetTemplate(model.CleanTextProtocol);
          ProtocolModel.SetErrorsTemplate(model.CleanTextErrorsProtocol);
        };

        ExecutionConfig.SaveExecutionEvent += async (model) =>
        {
          var service = new DataBaseConfiguration.Services.Settings.ExecutionService();
          await service.SaveExecutionAsync(model);
        };

        UserInterfaceConfig.SaveUserInterfaceEvent += async (model) =>
        {
          var service = new DataBaseConfiguration.Services.Settings.UserInterfaceService();
          await service.SaveUserInterfaceAsync(model);
        };

        DeviceDisplayConfig.DeviceDisplaySettingsSaved += async (model) =>
        {
          var service = new DataBaseConfiguration.Services.Settings.DeviceDisplayService();
          await service.SaveDeviceDisplayAsync(model);
        };
      }
      catch (Exception ex)
      {
        LogException(ex);
      }
    }
  }
}
