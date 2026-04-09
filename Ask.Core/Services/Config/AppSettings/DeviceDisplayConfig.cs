using Ask.Core.Shared.DTO.Settings;

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
    public static Action<DeviceDisplaySettingsDto>? DeviceDisplaySettingsSaved;

    /// <summary>
    /// Текущая модель настроек отображения.
    /// Хранит значения параметров визуализации.
    /// </summary>
    private static DeviceDisplaySettingsDto _settingsModel = new();

    #region Set.

    /// <summary>
    /// Устанавливает значение отображения машинных адресов точек.
    /// </summary>
    public static void SetMachineAddressVisibility(bool isVisible) => _settingsModel.ShowMachineAddresses = isVisible;

    /// <summary>
    /// Устанавливает значение отображения информации о подключении точек и шин.
    /// </summary>
    public static void SetConnectionInfoVisibility(bool isVisible) => _settingsModel.ShowConnectionInfo = isVisible;

    /// <summary>
    /// Устанавливает значение отображения параметров, которые задаются устройствам
    /// во время выполнения программы контроля.
    /// </summary>
    public static void SetExecutionParametersVisibility(bool isVisible) => _settingsModel.ShowDeviceExecutionParameters = isVisible;

    /// <summary>
    /// Устанавливает значение отображения результатов измерений,
    /// получаемых в процессе выполнения программы контроля.
    /// </summary>
    public static void SetMeasurementResultsVisibility(bool isVisible) => _settingsModel.ShowMeasurementResults = isVisible;

    /// <summary>
    /// Устанавливает значение отображения промежуточных результатов измерений,
    /// получаемых в процессе выполнения программы контроля.
    /// </summary>
    public static void SetIntermediateMeasurementResultsVisibility(bool isVisible) => _settingsModel.ShowIntermediateMeasurementResults = isVisible;

    public static async Task SetDeviceDisplaySettingsModel(DeviceDisplaySettingsDto model)
    {
      SetMachineAddressVisibility(model.ShowMachineAddresses);
      SetConnectionInfoVisibility(model.ShowConnectionInfo);
      SetExecutionParametersVisibility(model.ShowDeviceExecutionParameters);
      SetMeasurementResultsVisibility(model.ShowMeasurementResults);
      SetIntermediateMeasurementResultsVisibility(model.ShowIntermediateMeasurementResults);
    }

    #endregion
    #region Get.
    /// <summary>
    /// Возвращает признак отображения машинных адресов точек.
    /// </summary>
    public static bool GetMachineAddressVisibility() => _settingsModel.ShowMachineAddresses;

    /// <summary>
    /// Возвращает признак отображения сведений о подключении точек и шин.
    /// </summary>
    public static bool GetConnectionInfoVisibility() => _settingsModel.ShowConnectionInfo;

    /// <summary>
    /// Возвращает признак отображения параметров, которые задаются устройствам
    /// во время выполнения программы контроля.
    /// </summary>
    public static bool GetExecutionParametersVisibility() => _settingsModel.ShowDeviceExecutionParameters;

    /// <summary>
    /// Возвращает признак отображения результатов измерений.
    /// </summary>
    public static bool GetMeasurementResultsVisibility() => _settingsModel.ShowMeasurementResults;

    /// <summary>
    /// Возвращает признак отображения промежуточных результатов измерений.
    /// </summary>
    public static bool GetIntermediateMeasurementResultsVisibility() => _settingsModel.ShowIntermediateMeasurementResults;

    public static DeviceDisplaySettingsDto GetDeviceDisplayModel()
    {
      DeviceDisplaySettingsDto protocolModel = new DeviceDisplaySettingsDto();
      protocolModel.ShowMachineAddresses = _settingsModel.ShowMachineAddresses;
      protocolModel.ShowConnectionInfo = _settingsModel.ShowConnectionInfo;
      protocolModel.ShowDeviceExecutionParameters = _settingsModel.ShowDeviceExecutionParameters;
      protocolModel.ShowMeasurementResults = _settingsModel.ShowMeasurementResults;
      protocolModel.ShowIntermediateMeasurementResults = _settingsModel.ShowIntermediateMeasurementResults;
      return protocolModel;
    }
    #endregion


    /// <summary>
    /// Переносит значения из переданной модели в текущие параметры отображения
    /// и вызывает внешний обработчик сохранения.
    /// </summary>
    /// <param name="model">Модель с новыми значениями настроек.</param>
    public static void SaveSettings(DeviceDisplaySettingsDto model)
    {
      SetMachineAddressVisibility(model.ShowMachineAddresses);
      SetConnectionInfoVisibility(model.ShowConnectionInfo);
      SetExecutionParametersVisibility(model.ShowDeviceExecutionParameters);
      SetMeasurementResultsVisibility(model.ShowMeasurementResults);
      SetIntermediateMeasurementResultsVisibility(model.ShowIntermediateMeasurementResults);

      DeviceDisplaySettingsSaved?.Invoke(model);
    }
  }
}
