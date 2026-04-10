using Ask.Core.Shared.DTO.Devices.Base;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ask.Core.Shared.DTO.Devices.UninterruptiblePowerSupply;

/// <summary>
/// DTO источника бесперебойного питания (UPS).
/// Содержит параметры устройства без логики управления питанием и внешних зависимостей.
/// </summary>
[Table("UninterruptiblePowerSupplies")]
public class UninterruptiblePowerSupplyDto : AttachableDeviceDto
{
  /// <summary>
  /// Последний определённый системный путь к устройству.
  /// </summary>
  public string LastResolvedDevicePath { get; set; } = string.Empty;
}
