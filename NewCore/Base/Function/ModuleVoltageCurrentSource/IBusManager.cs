using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static NewCore.Enum.DeviceEnum;

namespace NewCore.Base.Function.ModuleVoltageCurrentSource
{
  public interface IBusManager
  {
    Task<bool> ConnectBusToPositiveAsync(SwitchingBus bus);
    Task<bool> ConnectBusToNegativeAsync(SwitchingBus bus);
    Task<bool> DisconnectBusToPositiveAsync(SwitchingBus bus);
    Task<bool> DisconnectBusToNegativeAsync(SwitchingBus bus);
  }
}
