using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ask.Core.Shared.DTO.Settings;

/// <summary>
/// DTO настроек отображения устройств.
/// Определяет, какие данные о устройствах и измерениях отображаются в интерфейсе.
/// </summary>
[Table("DeviceDisplaySettings")]
public class DeviceDisplaySettingsDto
{
  /// <summary>
  /// Идентификатор записи настроек.
  /// </summary>
  [Key]
  public int Id { get; set; }

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
