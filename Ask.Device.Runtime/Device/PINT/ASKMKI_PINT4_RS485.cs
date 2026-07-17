using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Device.Runtime.Device.ASKMKI;

namespace Ask.Device.Runtime.Device.PINT;

public sealed class ASKMKI_PINT4_RS485 : AskMkiPintBase
{
  public ASKMKI_PINT4_RS485()
    : base("АСК: ПИНТ4 RS-485", "Сетевой ПИНТ4 старого тестера АСК", DeviceType.PowerSourceModule, pintNumber: 4, voltageStep: 0.1, currentStep: 0.001, useBcdCode: false)
  {
  }
}
