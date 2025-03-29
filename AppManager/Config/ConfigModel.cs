using AppManager.Data.Execution;
using AppManager.Data.MeasurementError;
using AppManager.Data.Protocol;

namespace AppManager.Config
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
    static public async Task SetExecutionModelAsync(ExecutionModel executionModel)
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
    static public async Task SetProtocolModelAsync(ProtocolModel executionModel)
    {
      await ProtocolConfig.SetDeviceInfo(executionModel.ShowDeviceInfo);
      await ProtocolConfig.SetSaveProtocol(executionModel.AutoSaveProtocol);
      await ProtocolConfig.SetPrintProtocol(executionModel.AutoPrintProtocol);
      await ProtocolConfig.SetTimeStart(executionModel.DisplayOperationTime);
      await ProtocolConfig.SetShowDetailedProtocol(executionModel.ShowDetailedProtocol);
    }

    /// <summary>
    /// Устанавливает текущую модель погрешности измерений (MeasurementErrorModel).
    /// </summary>
    /// <param name="measurementErrorModels">Модели погрешности измерений.</param>
    public static void SetMeasurementErrorModels(List<MeasurementErrorModel> measurementErrorModels)
    {
      foreach (var measurementErrorModel in measurementErrorModels)
      {
        MeasurementErrorConfig.SetMeasurementErrorModel(measurementErrorModel);
      }
    }
  }
}
