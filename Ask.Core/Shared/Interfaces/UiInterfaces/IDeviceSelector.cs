using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;

namespace Ask.Core.Shared.Interfaces.UiInterfaces
{
  public interface IDeviceSelector
  {
    object? GetSelectedRelayDeviceByTypeSafe();
    DeviceType GetSelectedRelayDeviceType();
    Enum? GetSelectedSelfControlEnumUntypedSafe();
    IFastMeter? GetFastMeterSafe();
  }
}
