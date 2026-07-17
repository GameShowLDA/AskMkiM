using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Device.Runtime.Device.ASKMKI;

namespace Ask.Device.Runtime.Device.PINT;

public sealed class ASKMKI_PINT4_PUI : AskMkiPintBase
{
  public ASKMKI_PINT4_PUI()
    : base("АСК: ПИНТ4 ПУИ", "ПИНТ4 старого тестера АСК через регистр ПУИ", DeviceType.PowerSourceModule, pintNumber: 4, voltageStep: 0.1, currentStep: 0.001, useBcdCode: true)
  {
  }
}
