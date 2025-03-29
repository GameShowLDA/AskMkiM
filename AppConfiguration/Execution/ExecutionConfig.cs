using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppConfiguration.Execution
{
  /// <summary>
  /// Класс конфигурации выполнений режимов для <see cref="ExecutionConfig"/>.
  /// </summary>    /// <summary>
  /// Модель данных <see cref="MeasurementErrorModel"/> для режима ИЕ.
  /// </summary>
  public static class ExecutionConfig
  {
    static ExecutionModel ExecutionModel = new ExecutionModel();

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
        ExecutionModel.IdleModeExecution = enable;
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
        ExecutionModel.StopOnError = enable;
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
        ExecutionModel.StopOnError = enable;
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
        ExecutionModel.IsErrorSimulationMode = enable;
      });
    }

    #endregion

    #region Get.

    /// <summary>
    /// Проверяет, активен ли холостой режим.
    /// </summary>
    /// <returns>true, если включен; false, если выключен.</returns>
    public static async Task<bool> GetIsIdleModeEnabled() => await Task.Run(() => ExecutionModel.IdleModeExecution);

    /// <summary>
    /// Проверяет, установлен ли флаг остановки при ошибке.
    /// </summary>
    /// <returns>true, если включен; false, если выключен.</returns>
    public static async Task<bool> GetIsStopOnErrorEnabled() => await Task.Run(() => ExecutionModel.StopOnError);

    /// <summary>
    /// Возвращает, включена ли симуляция ошибок в холостом режиме.
    /// </summary>
    /// <returns>true, если включена; false, если выключена.</returns>
    public static async Task<bool> GetIsErrorSimulationEnabled() => await Task.Run(() => ExecutionModel.IsErrorSimulationMode);

    /// <summary>
    /// Возвращает, включена ли симуляция ошибок в холостом режиме.
    /// </summary>
    /// <returns>true, если включена; false, если выключена.</returns>
    public static async Task<bool> GetIsStepByStepModeEnabled() => await Task.Run(() => ExecutionModel.StopOnError);

    #endregion

    /// <summary>
    /// Перезаписывает конфигурацию выполнений.
    /// </summary>
    /// <returns></returns>
    public static async Task RewriteExecutionConfigAsync()
    {
      ExecutionModel executionModel = new ExecutionModel();
      executionModel.IdleModeExecution = ExecutionModel.IdleModeExecution;
      executionModel.StopOnError = ExecutionModel.StopOnError;
      executionModel.IsErrorSimulationMode = ExecutionModel.IsErrorSimulationMode;
      executionModel.StopOnError = ExecutionModel.StepByStepMode;

      ExecutionFileManager executionFileManager = new ExecutionFileManager(FileLocations.ExecutionConfigPath);
      await executionFileManager.RewriteFileAsync(executionModel);
    }
  }
}
