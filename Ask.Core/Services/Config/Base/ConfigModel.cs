using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.Entity.Settings;

namespace Ask.Core.Services.Config.Base
{
  /// <summary>
  /// Статический класс для хранения глобальных конфигурационных данных приложения.
  /// </summary>
  static public class ConfigModel
  {
    /// <summary>
    /// Устанавливает текущую модель выполнения (ExecutionModel).
    /// </summary>
    /// <param name="executionModel">Модель выполнения.</param>
    static public async Task SetExecutionModelAsync(SettingsExecutionModel executionModel)
    {
      await ExecutionConfig.SetStopOnError(executionModel.StopOnError);
      await ExecutionConfig.SetIsErrorSimulationMode(executionModel.IsErrorSimulationMode);
      await ExecutionConfig.SetStepByStepMode(executionModel.StepByStepMode);
      await ExecutionConfig.SetIdleMode(executionModel.IdleModeExecution);
    }

    /// <summary>
    /// Устанавливает текущую модель протокола (ProtocolModel).
    /// </summary>
    /// <param name="executionModel">Модель протокола.</param>
    static public async Task SetProtocolModelAsync(SettingsProtocolModel executionModel)
    {
      await ProtocolConfig.SetDeviceInfo(executionModel.ShowDeviceInfo);
      await ProtocolConfig.SetHeaderInfo(executionModel.ShowHeaderInfo);
      await ProtocolConfig.SetSaveProtocol(executionModel.AutoSaveProtocol);
      await ProtocolConfig.SetPrintProtocol(executionModel.AutoPrintProtocol);
      await ProtocolConfig.SetTimeStart(executionModel.DisplayOperationTime);
      await ProtocolConfig.SetShowDetailedProtocol(executionModel.ShowDetailedProtocol);
    }


    /// <summary>
    /// Устанавливает текущую модель протокола (ProtocolModel).
    /// </summary>
    /// <param name="executionModel">Модель протокола.</param>
    static public async Task SerParametrModelAsync(UserInterfaceModel executionModel)
    {
      await UserInterfaceConfig.SetLanguage(executionModel.Language);
      await UserInterfaceConfig.SetTheme(executionModel.Theme);
      await UserInterfaceConfig.SetSyntaxHighlighting(executionModel.UseSyntaxHighlighting);
    }
  }
}
