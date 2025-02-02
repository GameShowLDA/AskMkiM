using NewCore.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace NewCore.Interface
{
  /// <summary>
  /// Интерфейс для модуля коммутации реле
  /// </summary>
  public interface IRelaySwitchModule : IDevice
  {
    /// <summary>
    /// Номер менеджера шасси.
    /// </summary>
    public int NumberChassis { get; set; }

    /// <summary>
    /// Количество точек модуля.
    /// </summary>
    public int PointCount { get; set; }
  }
}
