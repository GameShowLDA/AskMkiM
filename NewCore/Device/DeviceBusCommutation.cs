using NewCore.Base.Device;
using NewCore.Base.Function.DBC;
using NewCore.Base.Interface.Main;
using NewCore.Function.DeviceBusCommutation;

namespace NewCore.Device
{
  public class DeviceBusCommutation : DeviceWithIP, ISwitchingDevice
  {
    public DeviceBusCommutation()
    {
      Name = "Устройство коммутации шин";
      Description = "Реализовать описание в NewCore.Device.DeviceBusCommutation";
      DeviceClass = GetType().FullName;
      SetFunction();
    }

    public void SetFunction()
    {
      BusManager = new BusManager(this);
      CapacitorManager = new CapacitorManager(this);
      ConnectorManager = new ConnectorManager(this);
      RelayManager = new RelayManager(this);
      ResistorManager = new ResistorManager(this);
      StateManager = new StateManager(this);
      SelfTestManager = new SelfTestCircuitChecker(this);
    }

    public IBusDeviceBusCommutation BusManager { get; set; }
    public ICapacitorDeviceBusCommutation CapacitorManager { get; set; }
    public IConnectorDeviceBusCommutation ConnectorManager { get; set; }
    public IRelayDeviceBusCommutation RelayManager { get; set; }
    public IResistorDeviceBusCommutation ResistorManager { get; set; }
    public IStateDeviceBusCommutation StateManager { get; set; }
    public ISelfTestChecker SelfTestManager { get; set; }


    public int NumberChassis { get; set; }
  }
}
