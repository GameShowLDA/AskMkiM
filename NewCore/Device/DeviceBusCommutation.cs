using System.Net;
using NewCore.Base;
using NewCore.Function.DeviceBusCommutation;
using NewCore.Interface;

namespace NewCore.Device
{
  public class DeviceBusCommutation : DeviceWithIP, ISwitchingDevice
  {
    public DeviceBusCommutation(IPAddress ip) : base(ip)
    {
      Name = "Устройство коммутации шин";
      Description = "Реализовать описание в NewCore.Device.DeviceBusCommutation";
    }

    public DeviceBusCommutation()
    {
      Name = "Устройство коммутации шин";
      Description = "Реализовать описание в NewCore.Device.DeviceBusCommutation";
    }

    public Functions Functions => new Functions(this);

    public int NumberChassis { get; set; }

    public override Task<bool> Initialize()

    {
      throw new NotImplementedException();
    }
  }
}
