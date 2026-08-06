using Ask.Core.Shared.DTO.Devices.Base;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ask.Core.Shared.DTO.Devices.SwitchingDevice;

/// <summary>
/// DTO устройства коммутации.
/// Содержит только базовые параметры устройства без логики управления.
/// </summary>
[Table("SwitchingDevices")]
public class SwitchingDeviceDto : AttachableDeviceDto
{
  /// <summary>
  /// Включает симуляцию сбоя неизмерительных команд этого устройства в холостом режиме.
  /// </summary>
  public bool IsHardwareFailureSimulationEnabled { get; set; }
}
