using Ask.Core.Shared.DTO.Devices.Base;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ask.Core.Shared.DTO.Devices.Breakdown;

/// <summary>
/// DTO пробойной установки.
/// Содержит параметры режимов и ограничений устройства без логики управления.
/// </summary>
[Table("BreakdownTesters")]
public class BreakdownTesterDto : AttachableDeviceDto
{
  /// <summary>
  /// Текущий режим работы пробойной установки.
  /// </summary>
  public BreakdownTypeMode Mode { get; set; }

  /// <summary>
  /// Диапазон установки напряжения ACW.
  /// </summary>
  public VoltageRange AcwVoltageRange { get; set; } = new();

  /// <summary>
  /// Диапазон установки напряжения DCW.
  /// </summary>
  public VoltageRange DcwVoltageRange { get; set; } = new();

  /// <summary>
  /// Диапазон установки напряжения IR.
  /// </summary>
  public VoltageRange IrVoltageRange { get; set; } = new();

  /// <summary>
  /// Сопротивление изоляции системы, ГОм.
  /// </summary>
  public int SystemInsulationResistanceGOhm { get; set; } = 60;
}
