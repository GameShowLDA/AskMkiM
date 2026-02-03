using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ask.Core.Shared.DTO.Devices.RelaySwitchModule
{
  public record DeviceConnectionInfo(SwitchingBusNew bus, string device);
}
