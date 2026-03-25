using Ask.Core.Shared.DTO.Devices.Base;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;

namespace Ask.Core.Shared.DTO.Devices.Breakdown;

/// <summary>
/// DTO пробойной установки.
/// Содержит параметры режимов и ограничений устройства без логики управления.
/// </summary>
public class BreakdownTesterDto : AttachableDeviceDto
{
  /// <summary>
  /// Текущий режим работы пробойной установки.
  /// </summary>
  public BreakdownTypeMode Mode { get; set; }

  /// <summary>
  /// Максимально допустимое напряжение для режима ПИ.
  /// </summary>
  public int PiMaxVoltage { get; set; }

  /// <summary>
  /// Максимально допустимое напряжение для режима СИ.
  /// </summary>
  public int SiMaxVoltage { get; set; }

  /// <summary>
  /// Минимальное напряжение для измерения сопротивления изоляции.
  /// </summary>
  public int IRMinVoltage { get; set; }
}