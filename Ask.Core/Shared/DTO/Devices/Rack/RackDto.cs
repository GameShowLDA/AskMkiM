using Ask.Core.Shared.DTO.Devices.Base;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ask.Core.Shared.DTO.Devices.Rack;

/// <summary>
/// DTO коммутационной стойки.
/// Содержит базовые параметры устройства без логики управления и runtime-состояния.
/// </summary>
[Table("Rack")]
public class RackDto : AttachableDeviceDto { }
