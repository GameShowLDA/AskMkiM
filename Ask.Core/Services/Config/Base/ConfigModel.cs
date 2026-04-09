using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.DTO.Settings;
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
    static public void SetProtocolModelAsync(SettingsProtocolDto executionModel)
    {
      ProtocolConfig.SetDeviceInfo(executionModel.ShowDeviceInfo);
      ProtocolConfig.SetHeaderInfo(executionModel.ShowHeaderInfo);
      ProtocolConfig.SetSaveProtocol(executionModel.AutoSaveProtocol);
      ProtocolConfig.SetPrintProtocol(executionModel.AutoPrintProtocol);
      ProtocolConfig.SetTimeStart(executionModel.DisplayOperationTime);
      ProtocolConfig.SetShowDetailedProtocol(executionModel.ShowDetailedProtocol);
      ProtocolConfig.SetCommandHeadersInProtocol(executionModel.UseCommandHeadersInProtocol);
    }


    /// <summary>
    /// Устанавливает текущую модель протокола (ProtocolModel).
    /// </summary>
    /// <param name="executionModel">Модель протокола.</param>
    static public async Task SerParametrModelAsync(UserInterfaceDto executionModel)
    {
      UserInterfaceConfig.SetLanguage(executionModel.Language);
      UserInterfaceConfig.SetTheme(executionModel.Theme);
      UserInterfaceConfig.SetSyntaxHighlighting(executionModel.UseSyntaxHighlighting);
      UserInterfaceConfig.SetCommandBodyBackgroundHighlighting(executionModel.UseCommandBodyBackgroundHighlighting);
      UserInterfaceConfig.SetChainPointBodyBackgroundHighlighting(executionModel.UseChainPointBodyBackgroundHighlighting);
      UserInterfaceConfig.SetTopMenuIcons(executionModel.UseTopMenuIcons);
      UserInterfaceConfig.SetCommandAutoCollapse(executionModel.UseCommandAutoCollapse);
    }
  }
}
