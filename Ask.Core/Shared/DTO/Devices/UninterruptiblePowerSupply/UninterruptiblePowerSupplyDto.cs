using Ask.Core.Shared.DTO.Devices.Base;

namespace Ask.Core.Shared.DTO.Devices.UninterruptiblePowerSupply;

/// <summary>
/// DTO источника бесперебойного питания (UPS).
/// Содержит параметры устройства без логики управления питанием и внешних зависимостей.
/// </summary>
public class UninterruptiblePowerSupplyDto : AttachableDeviceDto
{
  /// <summary>
  /// Последний определённый системный путь к устройству.
  /// </summary>
  public string LastResolvedDevicePath { get; set; } = string.Empty;
}