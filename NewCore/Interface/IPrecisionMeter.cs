using NewCore.Base;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace NewCore.Interface
{
  /// <summary>
  /// Интерфейс для точного измерителя
  /// </summary>
  public interface IPrecisionMeter : IDevice
  {
    /// <summary>
    /// Номер менеджера шасси.
    /// </summary>
    public int NumberChassis { get; set; }
  }

}
