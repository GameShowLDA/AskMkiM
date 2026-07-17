using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Device.Runtime.Device.ASKMKI;

namespace Ask.Device.Runtime.Device.PINT;

public sealed class ASKMKI_PINT3_B5108 : AskMkiPintBase
{
  public ASKMKI_PINT3_B5108()
    : base("АСК: ПИНТ3 B5108", "ПИНТ3 B5108 старого тестера АСК", DeviceType.PowerSourceModule, pintNumber: 3, voltageStep: 0.1, currentStep: 0.1, useBcdCode: false)
  {
  }
}
