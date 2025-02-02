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
  /// Интерфейс для модуля источника напряжения и тока
  /// </summary>
  public interface IPowerSourceModule : IDevice
  {
    /// <summary>
    /// Номер менеджера шасси.
    /// </summary>
    public int NumberChassis { get; set; }
  }
}
