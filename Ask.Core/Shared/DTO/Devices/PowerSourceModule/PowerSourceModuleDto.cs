using Ask.Core.Shared.DTO.Devices.Base;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ask.Core.Shared.DTO.Devices.PowerSourceModule
{
  /// <summary>
  /// DTO модуля источника питания.
  /// Содержит параметры устройства без логики управления и зависимостей.
  /// </summary>
  [Table("PowerSourceModules")]
  public class PowerSourceModuleDto : AttachableDeviceDto
  {
    /// <summary>
    /// JSON-строка с калибровочными коэффициентами по диапазонам сопротивления.
    /// </summary>
    public string? ResistanceCalibrationJson { get; set; }
  }
}
