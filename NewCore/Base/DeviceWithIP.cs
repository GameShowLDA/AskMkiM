using System.Net;
using static NewCore.Enum.DeviceEnum;

namespace NewCore.Base
{
  public abstract class DeviceWithIP : IDevice
  {
    public string Name { get; set; }
    public string Description { get; set; }
    public IPAddress IPAddress { get; set; }
    public int Number { get; set; }
    public string ConnectionDetails { get => GetIPAdress(IPAddress); set => SetIPAdress(); }

    public DeviceType DeviceType { get; set; }

    public abstract Task<bool> Initialize();

    public DeviceWithIP(IPAddress iPAddress)
    {
      IPAddress = iPAddress;
    }

    public DeviceWithIP()
    {
    }

    internal string GetIPAdress(IPAddress iPAddress)
    {
      return iPAddress.ToString();
    }

    internal IPAddress SetIPAdress()
    {
      var ipString = "192.168.1.1";
      if (IPAddress.TryParse(ipString, out IPAddress ipAddress))
      {
        return ipAddress;
      }
      else
      {
        // TODO: заменить на логер
        Console.WriteLine("Invalid IP Address format.");
        return IPAddress.None;
      }
    }
  }
}
