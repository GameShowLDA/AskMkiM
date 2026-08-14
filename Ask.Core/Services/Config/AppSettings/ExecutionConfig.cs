using Ask.Core.Services.EventCore.Adapters;
using Ask.Core.Shared.DTO.Settings;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;

namespace Ask.Core.Services.Config.AppSettings
{
  /// <summary>
  /// Класс конфигурации выполнений режимов для <see cref="ExecutionConfig"/>.
  /// </summary>    /// <summary>
  /// Модель данных <see cref="MeasurementErrorModel"/> для режима ИЕ.
  /// </summary>
  public static class ExecutionConfig
  {
    static public Func<SettingsExecutionDto, Task>? SaveExecutionAsyncEvent;

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
    public static void SetLegacyCompatibilityMode(bool enable) => SettingsExecutionModel.LegacyCompatibilityMode = enable;

    /// <summary>
    /// Включает или отключает проверку питания перед запуском выполнения.
    /// </summary>
    /// <param name="disable">Признак отключения проверки питания.</param>
    public static void SetDisablePowerCheck(bool disable) => SettingsExecutionModel.DisablePowerCheck = disable;

    /// <summary>
    /// Устанавливает режим симуляции ошибочных результатов измерений.
    /// </summary>
    /// <param name="type">Тип формируемого ошибочного результата.</param>
    public static void SetErroneousMeasurementType(TypeErroneousMeasurement type) =>
      SettingsExecutionModel.ErroneousMeasurementType = Enum.IsDefined(type)
        ? type
        : TypeErroneousMeasurement.None;

    /// <summary>
    /// Включает случайную симуляцию ошибочных измерений или отключает её.
    /// Сохранено для потребителей с булевым контрактом.
    /// </summary>
    public static void SetIsErrorSimulationMode(bool enable) =>
      SetErroneousMeasurementType(enable
        ? TypeErroneousMeasurement.Rnd
        : TypeErroneousMeasurement.None);

    /// <summary>
    /// Включает или выключает симуляцию аппаратных ошибок оборудования.
    /// </summary>
    /// <param name="enable">Состояние симуляции аппаратных ошибок.</param>
    public static void SetIsHardwareErrorSimulationMode(bool enable) =>
      SettingsExecutionModel.IsHardwareErrorSimulationMode = enable;

    public static Task SetExecutionModel(SettingsExecutionDto protocolModel)
    {
      SetIdleMode(protocolModel.IdleModeExecution);
      SetErroneousMeasurementType(protocolModel.ErroneousMeasurementType);
      SetIsHardwareErrorSimulationMode(protocolModel.IsHardwareErrorSimulationMode);
      SetStepByStepMode(protocolModel.StepByStepMode);
      SetStopOnError(protocolModel.StopOnError);
      SetLegacyCompatibilityMode(protocolModel.LegacyCompatibilityMode);
      SetDisablePowerCheck(protocolModel.DisablePowerCheck);

      return Task.CompletedTask;
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
    /// Возвращает текущий режим симуляции ошибочных результатов измерений.
    /// </summary>
    public static TypeErroneousMeasurement GetErroneousMeasurementType() =>
      SettingsExecutionModel?.ErroneousMeasurementType ?? TypeErroneousMeasurement.None;

    /// <summary>
    /// Проверяет, включена ли симуляция ошибочных результатов измерений.
    /// </summary>
    public static bool GetIsErroneousMeasurementEnabled() =>
      GetErroneousMeasurementType() != TypeErroneousMeasurement.None;

    /// <summary>
    /// Возвращает признак включения любого режима ошибочных измерений.
    /// Сохранено для потребителей с булевым контрактом.
    /// </summary>
    public static bool GetIsErrorSimulationEnabled() => GetIsErroneousMeasurementEnabled();

    /// <summary>
    /// Проверяет, включена ли симуляция аппаратных ошибок оборудования.
    /// </summary>
    /// <returns>
    /// <see langword="true"/>, если симуляция включена.
    /// В противном случае — <see langword="false"/>.
    /// </returns>
    public static bool GetIsHardwareErrorSimulationEnabled() =>
      SettingsExecutionModel?.IsHardwareErrorSimulationMode ?? false;

    /// <summary>
    /// Возвращает, включен ли пошаговый режим.
    /// </summary>
    /// <returns>true, если включен; false, если выключена.</returns>
    public static bool GetIsStepByStepModeEnabled() => SettingsExecutionModel?.StepByStepMode ?? false;
    public static bool GetIsLegacyCompatibilityModeEnabled() => SettingsExecutionModel?.LegacyCompatibilityMode ?? false;

    /// <summary>
    /// Проверяет, отключена ли проверка питания перед запуском выполнения.
    /// </summary>
    /// <returns><see langword="true"/>, если проверка питания отключена.</returns>
    public static bool GetIsPowerCheckDisabled() => SettingsExecutionModel?.DisablePowerCheck ?? false;

    public static Task<SettingsExecutionDto> GetExecitonModel()
      => Task.FromResult(GetExecutionModelSnapshot());

    /// <summary>
    /// Возвращает снимок текущих настроек выполнения.
    /// </summary>
    public static SettingsExecutionDto GetExecutionModelSnapshot()
    {
      return new SettingsExecutionDto
      {
        IdleModeExecution = SettingsExecutionModel.IdleModeExecution,
        ErroneousMeasurementType = SettingsExecutionModel.ErroneousMeasurementType,
        IsHardwareErrorSimulationMode = SettingsExecutionModel.IsHardwareErrorSimulationMode,
        StepByStepMode = SettingsExecutionModel.StepByStepMode,
        StopOnError = SettingsExecutionModel.StopOnError,
        LegacyCompatibilityMode = SettingsExecutionModel.LegacyCompatibilityMode,
        DisablePowerCheck = SettingsExecutionModel.DisablePowerCheck,
      };

    }
    #endregion

    public static async Task SaveExecutionModel(SettingsExecutionDto execution)
    {
      SetIdleMode(execution.IdleModeExecution);
      SetErroneousMeasurementType(execution.ErroneousMeasurementType);
      SetIsHardwareErrorSimulationMode(execution.IsHardwareErrorSimulationMode);
      SetStepByStepMode(execution.StepByStepMode);
      SetStopOnError(execution.StopOnError);
      SetLegacyCompatibilityMode(execution.LegacyCompatibilityMode);
      SetDisablePowerCheck(execution.DisablePowerCheck);

      await InvokeSaveExecutionAsync(execution);
    }

    private static async Task InvokeSaveExecutionAsync(SettingsExecutionDto execution)
    {
      if (SaveExecutionAsyncEvent == null)
      {
        return;
      }

      foreach (Func<SettingsExecutionDto, Task> handler in SaveExecutionAsyncEvent.GetInvocationList())
      {
        await handler(execution);
      }
    }
  }
}
