using Ask.Core.Services.EventCore.Adapters;
using Ask.Core.Shared.DTO.Settings;

namespace Ask.Core.Services.Config.AppSettings
{
  /// <summary>
  /// Класс конфигурации выполнений режимов для <see cref="ExecutionConfig"/>.
  /// </summary>    /// <summary>
  /// Модель данных <see cref="MeasurementErrorModel"/> для режима ИЕ.
  /// </summary>
  public static class ExecutionConfig
  {
    static public Action<SettingsExecutionDto> SaveExecutionEvent;

    private static SettingsExecutionDto SettingsExecutionModel = new SettingsExecutionDto();

    /// <summary>
    /// Событие на изменение холостого режима
    /// </summary>
    static public event EventHandler<bool> IdleModeChange;

    #region Set.

    /// <summary>
    /// Включает или выключает холостой режим.
    /// </summary>
    /// <param name="enable">true для включения, false для выключения.</param>
    public static void SetIdleMode(bool enable)
    {
      SettingsExecutionModel.IdleModeExecution = enable;
      IdleModeChange?.Invoke(null, enable);
    }

    /// <summary>
    /// Устанавливает режим по шагам.
    /// </summary>
    /// <param name="enable">true для включения, false для выключения.</param>
    public static void SetStepByStepMode(bool enable)
    {
      SettingsExecutionModel.StepByStepMode = enable;
      ExecutionEventAdapter.RaiseStepByStepModeChanged(enable);
    }

    /// <summary>
    /// Устанавливает флаг остановки выполнения при ошибке.
    /// </summary>
    /// <param name="enable">true для включения, false для выключения.</param>
    public static void SetStopOnError(bool enable) => SettingsExecutionModel.StopOnError = enable;

    /// <summary>
    /// Включает или выключает режим симуляции ошибок.
    /// </summary>
    /// <param name="enable">true для включения, false для выключения.</param>
    public static void SetIsErrorSimulationMode(bool enable) => SettingsExecutionModel.IsErrorSimulationMode = enable;

    public static async Task SetExecutionModel(SettingsExecutionDto protocolModel)
    {
      SetIdleMode(protocolModel.IdleModeExecution);
      SetIsErrorSimulationMode(protocolModel.IsErrorSimulationMode);
      SetStepByStepMode(protocolModel.StepByStepMode);
      SetStopOnError(protocolModel.StopOnError);
    }

    #endregion

    #region Get.

    /// <summary>
    /// Проверяет, активен ли холостой режим.
    /// </summary>
    /// <returns>true, если включен; false, если выключен.</returns>
    public static bool GetIsIdleModeEnabled() => SettingsExecutionModel?.IdleModeExecution ?? false;

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
    public static bool GetIsStepByStepModeEnabled() => SettingsExecutionModel?.StepByStepMode ?? false;

    public static async Task<SettingsExecutionDto> GetExecitonModel()
    {
      return await Task.Run(() =>
      {
        var executionModel = new SettingsExecutionDto
        {
          IdleModeExecution = SettingsExecutionModel.IdleModeExecution,
          IsErrorSimulationMode = SettingsExecutionModel.IsErrorSimulationMode,
          StepByStepMode = SettingsExecutionModel.StepByStepMode,
          StopOnError = SettingsExecutionModel.StopOnError
        };
        return executionModel;
      });
    }
    #endregion

    public static async Task SaveExecutionModel(SettingsExecutionDto execution)
    {
      SetIdleMode(execution.IdleModeExecution);
      SetIsErrorSimulationMode(execution.IsErrorSimulationMode);
      SetStepByStepMode(execution.StepByStepMode);
      SetStopOnError(execution.StopOnError);

      SaveExecutionEvent?.Invoke(execution);
    }
  }
}
