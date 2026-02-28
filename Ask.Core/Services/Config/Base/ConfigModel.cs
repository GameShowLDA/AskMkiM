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
    static public void SetExecutionModelAsync(SettingsExecutionModel executionModel)
    {
      ExecutionConfig.SetStopOnError(executionModel.StopOnError);
      ExecutionConfig.SetIsErrorSimulationMode(executionModel.IsErrorSimulationMode);
      ExecutionConfig.SetStepByStepMode(executionModel.StepByStepMode);
      ExecutionConfig.SetIdleMode(executionModel.IdleModeExecution);
    }

    /// <summary>
    /// Устанавливает текущую модель протокола (ProtocolModel).
    /// </summary>
    /// <param name="executionModel">Модель протокола.</param>
    static public void SetProtocolModelAsync(SettingsProtocolModel executionModel)
    {
      ProtocolConfig.SetDeviceInfo(executionModel.ShowDeviceInfo);
      ProtocolConfig.SetHeaderInfo(executionModel.ShowHeaderInfo);
      ProtocolConfig.SetSaveProtocol(executionModel.AutoSaveProtocol);
      ProtocolConfig.SetPrintProtocol(executionModel.AutoPrintProtocol);
      ProtocolConfig.SetTimeStart(executionModel.DisplayOperationTime);
      ProtocolConfig.SetShowDetailedProtocol(executionModel.ShowDetailedProtocol);
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
      await UserInterfaceConfig.SetCommandBodyBackgroundHighlighting(executionModel.UseCommandBodyBackgroundHighlighting);
      await UserInterfaceConfig.SetChainPointBodyBackgroundHighlighting(executionModel.UseChainPointBodyBackgroundHighlighting);
    }
  }
}
