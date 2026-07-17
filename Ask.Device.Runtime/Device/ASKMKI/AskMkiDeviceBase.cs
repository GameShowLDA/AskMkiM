using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Device.Runtime.Base.Connected;
using Ask.Device.Runtime.Base.DeviceProtocol;

namespace Ask.Device.Runtime.Device.ASKMKI;

public abstract class AskMkiDeviceBase : DeviceWithCOM, IAskMkiAttachableDevice
{
  protected AskMkiDeviceBase(string name, string description, DeviceType deviceType)
  {
    Name = name;
    Description = description;
    DeviceType = deviceType;
    DeviceClass = GetType().FullName ?? string.Empty;
    ConnectableManager = new Transport(this);
    IsAttachableDevice = true;
  }

  public int NumberChassis { get; set; }
}
