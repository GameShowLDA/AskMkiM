using System.Net;
using static NewCore.Enum.DeviceEnum;

namespace NewCore.Base
{
  public abstract class DeviceWithIP : IDevice
  {
    public string Name { get; set; }
    public string Description { get; set; }
    public ConnectionType ConnectionType { get; set; } = ConnectionType.IP;
    public IPAddress IPAddress { get; set; }
    public int Number { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public string ConnectionDetails { get; set; }

    public DeviceType DeviceType => throw new NotImplementedException();

    public abstract Task<bool> Initialize();
    public DeviceWithIP(IPAddress iPAddress)
    {
      IPAddress = iPAddress;
    }
  }
}
