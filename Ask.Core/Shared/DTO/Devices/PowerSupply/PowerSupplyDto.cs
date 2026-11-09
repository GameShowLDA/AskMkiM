using Ask.Core.Shared.DTO.Devices.Base;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ask.Core.Shared.DTO.Devices.PowerSupply;

/// <summary>
/// Содержит сохранённую конфигурацию внешнего источника питания.
/// </summary>
[Table("PowerSupplies")]
public class PowerSupplyDto : AttachableDeviceDto
{
  /// <summary>
  /// TCP-порт управления источником питания.
  /// </summary>
  public int Port { get; set; } = 80;
}
