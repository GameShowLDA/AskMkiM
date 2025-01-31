using NewCore.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppConfig.Config
{
  public class DeviceConfig
  {
    public List<IDevice> ChassisManagers { get; set; } = new();
    public List<IDevice> BusSwitchingDevices { get; set; } = new();
    public List<IDevice> RelaySwitchingModules { get; set; } = new();
    public List<IDevice> VoltageCurrentSourceModules { get; set; } = new();
    public List<IDevice> FastMeters { get; set; } = new();
    public List<IDevice> PreciseMeters { get; set; } = new();
    public List<IDevice> BreakdownSetups { get; set; } = new();
  }
}
