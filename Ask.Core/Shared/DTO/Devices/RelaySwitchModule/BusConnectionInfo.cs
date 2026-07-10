using Ask.Core.Shared.Metadata.Enums.DeviceEnums;

namespace Ask.Core.Shared.DTO.Devices.RelaySwitchModule
{
  public record BusConnectionInfo(SwitchingBus Bus, bool IsConnected) { }
}
