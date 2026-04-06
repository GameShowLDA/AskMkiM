namespace Ask.Core.Shared.DTO.Devices.Base;

/// <summary>
/// DTO для устройств, подключаемых к шасси.
/// Расширяет базовый DTO устройства дополнительной информацией о подключении.
/// </summary>
public class AttachableDeviceDto : DeviceDto
{
  /// <summary>
  /// Номер шасси, к которому подключено устройство.
  /// </summary>
  public int NumberChassis { get; set; }
}