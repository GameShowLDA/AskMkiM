using Ask.Core.Services.EventCore.Adapters;
using Ask.Core.Shared.Entity.Settings;

namespace Ask.Core.Services.Config.AppSettings
{
  /// <summary>
  /// Класс конфигурации выполнений режимов для <see cref="ExecutionConfig"/>.
  /// </summary>    /// <summary>
  /// Модель данных <see cref="MeasurementErrorModel"/> для режима ИЕ.
  /// </summary>
  public static class ExecutionConfig
  {
    static public Action<SettingsExecutionModel> SaveExecutionEvent;

    private static SettingsExecutionModel SettingsExecutionModel = new SettingsExecutionModel();

    /// <summary>
    /// Событие на изменение холостого режима
    /// </summary>
    static public event EventHandler<bool> IdleModeChange;

    #region Set.

    /// <summary>
    /// Включает или выключает холостой режим.
    /// </summary>
    /// <param name="enable">true для включения, false для выключения.</param>
    public static async Task SetIdleMode(bool enable)
    {
      await Task.Run(() =>
      {
        SettingsExecutionModel.IdleModeExecution = enable;
        IdleModeChange?.Invoke(null, enable);
      });
    }

    /// <summary>
    /// Устанавливает режим по шагам.
    /// </summary>
    /// <param name="enable">true для включения, false для выключения.</param>
    public static async Task SetStepByStepMode(bool enable)
    {
      await Task.Run(() =>
      {
        SettingsExecutionModel.StepByStepMode = enable;
        ExecutionEventAdapter.RaiseStepByStepModeChanged(enable);
      });
    }

    /// <summary>
    /// Устанавливает флаг остановки выполнения при ошибке.
    /// </summary>
    /// <param name="enable">true для включения, false для выключения.</param>
    public static async Task SetStopOnError(bool enable)
    {
      await Task.Run(() =>
      {
        SettingsExecutionModel.StopOnError = enable;
      });
    }

    /// <summary>
    /// Включает или выключает режим симуляции ошибок.
    /// </summary>
    /// <param name="enable">true для включения, false для выключения.</param>
    public static async Task SetIsErrorSimulationMode(bool enable)
    {
      await Task.Run(() =>
      {
        SettingsExecutionModel.IsErrorSimulationMode = enable;
      });
    }

    public static async Task SetExecutionModel(SettingsExecutionModel protocolModel)
    {
      await Task.Run(async () =>
      {
        await SetIdleMode(protocolModel.IdleModeExecution);
        await SetIsErrorSimulationMode(protocolModel.IsErrorSimulationMode);
        await SetStepByStepMode(protocolModel.StepByStepMode);
        await SetStopOnError(protocolModel.StopOnError);
      });
    }

    #endregion

    #region Get.

    /// <summary>
    /// Проверяет, активен ли холостой режим.
    /// </summary>
    /// <returns>true, если включен; false, если выключен.</returns>
    public static Task<bool> GetIsIdleModeEnabled() => Task.FromResult(SettingsExecutionModel?.IdleModeExecution ?? false);

    /// <summary>
    /// Проверяет, установлен ли флаг остановки при ошибке.
    /// </summary>
    /// <returns>true, если включен; false, если выключен.</returns>
    public static Task<bool> GetIsStopOnErrorEnabled() => Task.FromResult(SettingsExecutionModel?.StopOnError ?? false);

    /// <summary>
    /// Возвращает, включена ли симуляция ошибок в холостом режиме.
    /// </summary>
    /// <returns>true, если включена; false, если выключена.</returns>
    public static Task<bool> GetIsErrorSimulationEnabled() => Task.FromResult(SettingsExecutionModel?.IsErrorSimulationMode ?? false);

    /// <summary>
    /// Возвращает, включен ли пошаговый режим.
    /// </summary>
    /// <returns>true, если включен; false, если выключена.</returns>
    public static Task<bool> GetIsStepByStepModeEnabled() => Task.FromResult(SettingsExecutionModel?.StepByStepMode ?? false);

    public static async Task<SettingsExecutionModel> GetExecitonModel()
    {
      return await Task.Run(() =>
      {
        SettingsExecutionModel executionModel = new SettingsExecutionModel();
        executionModel.IdleModeExecution = SettingsExecutionModel.IdleModeExecution;
        executionModel.IsErrorSimulationMode = SettingsExecutionModel.IsErrorSimulationMode;
        executionModel.StepByStepMode = SettingsExecutionModel.StepByStepMode;
        executionModel.StopOnError = SettingsExecutionModel.StopOnError;
        return executionModel;
      });
    }
    #endregion

    public static async Task SaveExecutionModel(SettingsExecutionModel execution)
    {
      await Task.Run(async () =>
      {
        await SetIdleMode(execution.IdleModeExecution);
        await SetIsErrorSimulationMode(execution.IsErrorSimulationMode);
        await SetStepByStepMode(execution.StepByStepMode);
        await SetStopOnError(execution.StopOnError);
      });

      SaveExecutionEvent?.Invoke(execution);
    }
  }
}
