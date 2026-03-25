namespace Ask.Core.Shared.DTO.Settings;

/// <summary>
/// DTO настроек отображения устройств.
/// Определяет, какие данные о устройствах и измерениях отображаются в интерфейсе.
/// </summary>
public class DeviceDisplaySettingsDto
{
  /// <summary>
  /// Отображать машинные адреса точек.
  /// </summary>
  public bool ShowMachineAddresses { get; set; }

  /// <summary>
  /// Отображать информацию о соединениях точек и шин.
  /// </summary>
  public bool ShowConnectionInfo { get; set; }

  /// <summary>
  /// Отображать параметры, устанавливаемые на устройства во время выполнения.
  /// </summary>
  public bool ShowDeviceExecutionParameters { get; set; }

  /// <summary>
  /// Отображать результаты измерений.
  /// </summary>
  public bool ShowMeasurementResults { get; set; }

  /// <summary>
  /// Отображать промежуточные результаты измерений.
  /// </summary>
  public bool ShowIntermediateMeasurementResults { get; set; }
}