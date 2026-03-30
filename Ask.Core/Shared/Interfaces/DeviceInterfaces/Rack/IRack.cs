using Ask.Core.Shared.DTO.Devices.UninterruptiblePowerSupply;

namespace Ask.Core.Shared.Interfaces.DeviceInterfaces.Rack
{
  public interface IRack : IAttachableDevice, IHeadUnit, IDeviceToDtoConverter<UninterruptiblePowerSupplyDto> { }
}
