using AppConfig.Data.Execution;

namespace AppConfig.Config
{
  /// <summary>
  /// Класс конфигурации для <see cref="ExecutionConfig"/>.
  /// </summary>    /// <summary>
  /// Модель данных <see cref="MeasurementErrorModel"/> для режима ИЕ.
  /// </summary>
  public static class ExecutionConfig
  {
    #region Properties.

    /// <summary>
    /// Флаг, указывающий, активен ли холостой режим.
    /// </summary>
    static private bool IsIdleModeActive { get; set; }

    /// <summary>
    /// Флаг, указывающий, нужно ли останавливать выполнение при ошибке.
    /// </summary>
    static private bool StopOnError { get; set; }

    /// <summary>
    /// Флаг, указывающий, активен ли режим симуляции ошибок в холостом режиме.
    /// </summary>
    static private bool IsErrorSimulationActive { get; set; }

    /// <summary>
    /// Флаг, указывающий, активен ли пошаговый режим.
    /// </summary>
    static private bool IsStepByStepModeActive { get; set; }
    #endregion

    #region Set.

    /// <summary>
    /// Включает или выключает холостой режим.
    /// </summary>
    /// <param name="enable">true для включения, false для выключения.</param>
    public static async Task SetIdleMode(bool enable)
    {
      await Task.Run(() =>
      {
        IsIdleModeActive = enable;
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
        IsStepByStepModeActive = enable;
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
        StopOnError = enable;
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
        IsErrorSimulationActive = enable;
      });
    }

    #endregion

    #region Get.

    /// <summary>
    /// Проверяет, активен ли холостой режим.
    /// </summary>
    /// <returns>true, если включен; false, если выключен.</returns>
    public static async Task<bool> GetIsIdleModeEnabled() => await Task.Run(() => IsIdleModeActive);

    /// <summary>
    /// Проверяет, установлен ли флаг остановки при ошибке.
    /// </summary>
    /// <returns>true, если включен; false, если выключен.</returns>
    public static async Task<bool> GetIsStopOnErrorEnabled() => await Task.Run(() => StopOnError);

    /// <summary>
    /// Возвращает, включена ли симуляция ошибок в холостом режиме.
    /// </summary>
    /// <returns>true, если включена; false, если выключена.</returns>
    public static async Task<bool> GetIsErrorSimulationEnabled() => await Task.Run(() => IsErrorSimulationActive);

    /// <summary>
    /// Возвращает, включена ли симуляция ошибок в холостом режиме.
    /// </summary>
    /// <returns>true, если включена; false, если выключена.</returns>
    public static async Task<bool> GetIsStepByStepModeEnabled() => await Task.Run(() => IsStepByStepModeActive);

    #endregion

    public static async Task RewriteExecutionConfigAsync()
    {
      ExecutionModel executionModel = new ExecutionModel();
      executionModel.IdleModeExecution = IsIdleModeActive;
      executionModel.StopOnError = StopOnError;
      executionModel.IsErrorSimulationMode = IsErrorSimulationActive;
      executionModel.StopOnError = IsStepByStepModeActive;

      ExecutionFileManager executionFileManager = new ExecutionFileManager(FileLocations.ExecutionConfigPath);
      await executionFileManager.RewriteFileAsync(executionModel);
    }
  }
}
