using Ask.Core.Shared.DTO.Devices.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ask.Core.Shared.DTO.Devices.PowerSourceModule
{
  /// <summary>
  /// DTO модуля источника питания.
  /// Содержит параметры устройства без логики управления и зависимостей.
  /// </summary>
  public class PowerSourceModuleDto : AttachableDeviceDto
  {
    /// <summary>
    /// JSON-строка с калибровочными коэффициентами по диапазонам сопротивления.
    /// </summary>
    public string? ResistanceCalibrationJson { get; set; }
  }
}
