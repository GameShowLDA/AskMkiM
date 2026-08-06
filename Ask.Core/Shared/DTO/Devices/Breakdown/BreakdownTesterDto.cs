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
  /// Включает симуляцию сбоя неизмерительных команд этого устройства в холостом режиме.
  /// </summary>
  public bool IsHardwareFailureSimulationEnabled { get; set; }

  /// <summary>
  /// Текущий режим работы пробойной установки.
  /// </summary>
  public BreakdownTypeMode Mode { get; set; }

  /// <summary>
  /// Максимально допустимое напряжение для режима ПИ.
  /// </summary>
  public int AcwMaxVoltage { get; set; }
  public int DcwMaxVoltage { get; set; }

  /// <summary>
  /// Максимально допустимое напряжение для режима СИ.
  /// </summary>
  public int SiMaxVoltage { get; set; }

  /// <summary>
  /// Минимальное напряжение для измерения сопротивления изоляции.
  /// </summary>
  public int IRMinVoltage { get; set; }

  /// <summary>
  /// Сопротивление изоляции системы, ГОм.
  /// </summary>
  public int SystemInsulationResistanceGOhm { get; set; } = 60;
}
