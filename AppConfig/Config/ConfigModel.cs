using AppConfig.Data.Execution;
using AppConfig.Data.MeasurementError;
using AppConfig.Data.Protocol;

namespace AppConfig.Config
{
  /// <summary>
  /// Статический класс для хранения глобальных конфигурационных данных приложения.
  /// </summary>
  static public class ConfigModel
  {
    /// <summary>
    /// Устанавливает текущую модель выполнения (ExecutionModel).
    /// </summary>
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
    static public async Task SetProtocolModelAsync(ProtocolModel executionModel)
    {
      await ProtocolConfig.SetDeviceInfo(executionModel.DeviceInfo);
      await ProtocolConfig.SetSaveProtocol(executionModel.SaveProtocol);
      await ProtocolConfig.SetPrintProtocol(executionModel.PrintProtocol);
      await ProtocolConfig.SetTimeStart(executionModel.StartTime);
      await ProtocolConfig.SetShowDetailedProtocol(executionModel.ShowDetailedProtocol);
    }

    /// <summary>
    /// Устанавливает текущую модель погрешности измерений (MeasurementErrorModel).
    /// </summary>
    public static void SetMeasurementErrorModels(List<MeasurementErrorModel> measurementErrorModels)
    {
      foreach (var measurementErrorModel in measurementErrorModels)
      {
        MeasurementErrorConfig.SetMeasurementErrorModel(measurementErrorModel);
      }
    }
  }
}
