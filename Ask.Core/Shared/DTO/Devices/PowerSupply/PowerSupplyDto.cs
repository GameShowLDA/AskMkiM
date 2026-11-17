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

  /// <summary>
  /// Минимальная допустимая уставка напряжения в вольтах.
  /// </summary>
  public decimal MinVoltage { get; set; } = 0m;

  /// <summary>
  /// Максимальная допустимая уставка напряжения в вольтах.
  /// </summary>
  public decimal MaxVoltage { get; set; } = 150m;

  /// <summary>
  /// Программный шаг уставки напряжения в вольтах.
  /// </summary>
  public decimal VoltageStep { get; set; } = 0.1m;

  /// <summary>
  /// Минимальная допустимая уставка ограничения тока в амперах.
  /// </summary>
  public decimal MinCurrent { get; set; } = 0m;

  /// <summary>
  /// Максимальная допустимая уставка ограничения тока в амперах.
  /// </summary>
  public decimal MaxCurrent { get; set; } = 5m;

  /// <summary>
  /// Программный шаг уставки тока в амперах.
  /// </summary>
  public decimal CurrentStep { get; set; } = 0.001m;
}
