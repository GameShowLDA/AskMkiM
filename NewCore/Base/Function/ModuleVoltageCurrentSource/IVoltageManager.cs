using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static NewCore.Enum.DeviceEnum;

namespace NewCore.Base.Function.ModuleVoltageCurrentSource
{
  public interface IVoltageManager
  {
    Task SetSourceVoltageAsync(VoltageSources voltageSources);
    Task SetVoltageLevelAsync(int integerPart, int decimalPart);
  }
}
