using Ask.Core.Shared.DTO.Devices.Base;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ask.Core.Shared.DTO.Devices.RelaySwitchModule;

/// <summary>
/// DTO модуля коммутации реле.
/// Содержит только данные устройства без логики управления и зависимостей.
/// </summary>
[Table("RelaySwitchModules")]
public class RelaySwitchModuleDto : AttachableDeviceDto
{
  /// <summary>
  /// Номер стойки, в которой установлен модуль.
  /// </summary>
  public int NumberRack { get; set; }

  /// <summary>
  /// Общее количество точек коммутации модуля.
  /// </summary>
  public int PointCount { get; set; }

  /// <summary>
  /// Тип структурной шины модуля.
  /// </summary>
  public SwitchingBusNew BusType { get; set; }

  /// <summary>
  /// Сопротивление коммутатора.
  /// </summary>
  public double SwitchResistance { get; set; }

  /// <summary>
  /// Собственная ёмкость коммутатора.
  /// </summary>
  public double SwitchCapacitance { get; set; }
}
