using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Device.Runtime.Function.Base.Status;

namespace Ask.Device.Runtime.Base.Device
{
  public abstract class DeviceWithTcpIp : DeviceWithIP, IDevice
  {
    public DeviceWithTcpIp()
    {
      ConnectionInfo = new ConnectionInfoBase(this, ConnectionType.IP_TCP);
    }
  }
}
