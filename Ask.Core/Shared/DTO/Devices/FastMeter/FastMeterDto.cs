using Ask.Core.Shared.DTO.Devices.Base;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ask.Core.Shared.DTO.Devices.FastMeter;

/// <summary>
/// DTO быстрого измерителя.
/// Содержит параметры устройства без логики измерений и управляющих зависимостей.
/// </summary>
[Table("FastMeters")]
public class FastMeterDto : AttachableDeviceDto
{
  /// <summary>
  /// Текущий режим работы мультиметра.
  /// </summary>
  public MultimeterTypeMode TypeMode { get; set; }

  /// <summary>
  /// Максимальное сопротивление (Ом), при котором фиксируется прозвонка.
  /// </summary>
  public int MaxContinuityResistance { get; set; }
}
