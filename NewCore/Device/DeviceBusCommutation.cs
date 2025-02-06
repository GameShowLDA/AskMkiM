using System.Net;
using NewCore.Base;
using NewCore.Function.DeviceBusCommutation;

namespace NewCore.Device
{
  public class DeviceBusCommutation : DeviceWithIP
  {
    public DeviceBusCommutation(IPAddress ip) : base(ip)
    {
      Name = "Устройство коммутации шин";
      Description = "Реализовать описание в NewCore.Device.DeviceBusCommutation";
    }

    public Functions Functions => new Functions(this);

    public override Task<bool> Initialize()

    {
      throw new NotImplementedException();
    }
  }
}
