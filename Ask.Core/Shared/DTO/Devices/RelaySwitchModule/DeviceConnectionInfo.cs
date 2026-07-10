using Ask.Core.Shared.Metadata.Enums.DeviceEnums;

namespace Ask.Core.Shared.DTO.Devices.RelaySwitchModule
{
  public record DeviceConnectionInfo(SwitchingBusNew bus, string device);
}
