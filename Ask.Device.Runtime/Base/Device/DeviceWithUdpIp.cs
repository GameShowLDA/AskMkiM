using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Device.Runtime.Function.Base.Status;

namespace Ask.Device.Runtime.Base.Device
{
  public abstract class DeviceWithUdpIp : DeviceWithIP, IDevice
  {
    public DeviceWithUdpIp()
    {
      ConnectionInfo = new ConnectionInfoBase(this, ConnectionType.IP_UDP);
    }
  }
}
