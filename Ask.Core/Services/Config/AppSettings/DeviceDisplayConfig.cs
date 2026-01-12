using Ask.Core.Shared.Entity.Settings;

namespace Ask.Core.Services.Config.AppSettings
{
  /// <summary>
  /// Предоставляет функциональность для управления параметрами отображения
  /// информации об устройствах в системе АСК-МКИ-М. 
  /// Позволяет изменять и получать настройки визуализации,
  /// а также выполнять их сохранение через внешний обработчик.
  /// </summary>
  public static class DeviceDisplayConfig
  {
    /// <summary>
    /// Событие, возникающее при сохранении набора настроек отображения.
    /// Предоставляет внешний доступ к итоговой модели настроек.
    /// </summary>
    public static Action<DeviceDisplaySettingsModel>? DeviceDisplaySettingsSaved;

    /// <summary>
    /// Текущая модель настроек отображения.
    /// Хранит значения параметров визуализации.
    /// </summary>
    private static DeviceDisplaySettingsModel _settingsModel = new();

    #region Set.

    /// <summary>
    /// Устанавливает значение отображения машинных адресов точек.
    /// </summary>
    public static Task SetMachineAddressVisibilityAsync(bool isVisible)
    {
      _settingsModel.ShowMachineAddresses = isVisible;
      return Task.CompletedTask;
    }

    /// <summary>
    /// Устанавливает значение отображения информации о подключении точек и шин.
    /// </summary>
    public static Task SetConnectionInfoVisibilityAsync(bool isVisible)
    {
      _settingsModel.ShowConnectionInfo = isVisible;
      return Task.CompletedTask;
    }

    /// <summary>
    /// Устанавливает значение отображения параметров, которые задаются устройствам
    /// во время выполнения программы контроля.
    /// </summary>
    public static Task SetExecutionParametersVisibilityAsync(bool isVisible)
    {
      _settingsModel.ShowDeviceExecutionParameters = isVisible;
      return Task.CompletedTask;
    }

    /// <summary>
    /// Устанавливает значение отображения результатов измерений,
    /// получаемых в процессе выполнения программы контроля.
    /// </summary>
    public static Task SetMeasurementResultsVisibilityAsync(bool isVisible)
    {
      _settingsModel.ShowMeasurementResults = isVisible;
      return Task.CompletedTask;
    }

    public static async Task SetDeviceDisplaySettingsModel(DeviceDisplaySettingsModel model)
    {
      await Task.Run(async () =>
      {
        await SetMachineAddressVisibilityAsync(model.ShowMachineAddresses);
        await SetConnectionInfoVisibilityAsync(model.ShowConnectionInfo);
        await SetExecutionParametersVisibilityAsync(model.ShowDeviceExecutionParameters);
        await SetMeasurementResultsVisibilityAsync(model.ShowMeasurementResults);
      });
    }

    #endregion
    #region Get.
    /// <summary>
    /// Возвращает признак отображения машинных адресов точек.
    /// </summary>
    public static Task<bool> GetMachineAddressVisibilityAsync() =>
        Task.FromResult(_settingsModel.ShowMachineAddresses);

    /// <summary>
    /// Возвращает признак отображения сведений о подключении точек и шин.
    /// </summary>
    public static Task<bool> GetConnectionInfoVisibilityAsync() =>
        Task.FromResult(_settingsModel.ShowConnectionInfo);

    /// <summary>
    /// Возвращает признак отображения параметров, которые задаются устройствам
    /// во время выполнения программы контроля.
    /// </summary>
    public static Task<bool> GetExecutionParametersVisibilityAsync() =>
        Task.FromResult(_settingsModel.ShowDeviceExecutionParameters);

    /// <summary>
    /// Возвращает признак отображения результатов измерений.
    /// </summary>
    public static Task<bool> GetMeasurementResultsVisibilityAsync() =>
        Task.FromResult(_settingsModel.ShowMeasurementResults);

    public static async Task<DeviceDisplaySettingsModel> GetDeviceDisplayModel()
    {
      return await Task.Run(() =>
      {
        DeviceDisplaySettingsModel protocolModel = new DeviceDisplaySettingsModel();
        protocolModel.ShowMachineAddresses = _settingsModel.ShowMachineAddresses;
        protocolModel.ShowConnectionInfo = _settingsModel.ShowConnectionInfo;
        protocolModel.ShowDeviceExecutionParameters = _settingsModel.ShowDeviceExecutionParameters;
        protocolModel.ShowMeasurementResults = _settingsModel.ShowMeasurementResults;
        return protocolModel;
      });
    }
    #endregion


    /// <summary>
    /// Переносит значения из переданной модели в текущие параметры отображения
    /// и вызывает внешний обработчик сохранения.
    /// </summary>
    /// <param name="model">Модель с новыми значениями настроек.</param>
    public static async Task SaveSettingsAsync(DeviceDisplaySettingsModel model)
    {
      await SetMachineAddressVisibilityAsync(model.ShowMachineAddresses);
      await SetConnectionInfoVisibilityAsync(model.ShowConnectionInfo);
      await SetExecutionParametersVisibilityAsync(model.ShowDeviceExecutionParameters);
      await SetMeasurementResultsVisibilityAsync(model.ShowMeasurementResults);

      DeviceDisplaySettingsSaved?.Invoke(model);
    }
  }
}
