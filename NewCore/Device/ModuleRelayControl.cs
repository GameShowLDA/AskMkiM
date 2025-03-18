using System.Net;
using NewCore.Base.Device;
using NewCore.Base.Function.ModuleRelayControl;
using NewCore.Base.Interface.Main;
using NewCore.Enum;
using NewCore.Function.ModuleRelayControl;

namespace NewCore.Device
{
  public class ModuleRelayControl : DeviceWithIP, IRelaySwitchModule
  {
    public ModuleRelayControl() 
    {
      BusManager = new BusManager(this);
      MeterManager = new MeterManager(this);
      PointManager = new PointManager(this);
      StateManager = new StateManager(this);
    }

    public int Number { get; set; }
    public int NumberRack { get; set; }
    public string ConnectionDetails { get; set; }

    public DeviceEnum.DeviceType DeviceType => DeviceEnum.DeviceType.ChassisManager;

    public int NumberChassis { get; set; }
    public int PointCount { get; set; }
    public IBusManager BusManager { get; set; }
    public IMeterManager MeterManager { get; set; }
    public IPointManager PointManager { get; set; }
    public IStateManager StateManager { get; set; }
  }
}
