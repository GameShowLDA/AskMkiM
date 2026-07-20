using Ask.Core.Shared.DTO.Devices.Base;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Device.Runtime.Device.ASKMKI;

namespace Ask.Device.Runtime.Device.PINT;

public sealed class ASKMKI_PINT4_PUI : AskMkiPintBase
{
  public ASKMKI_PINT4_PUI(double voltageStep = 0.1, double currentStep = 0.001)
    : base("АСК: ПИНТ4 ПУИ", "ПИНТ4 старого тестера АСК через регистр ПУИ", DeviceType.PowerSourceModule, pintNumber: 4, voltageStep: voltageStep, currentStep: currentStep, useBcdCode: true)
  {
  }

  public override ComPortSettings DefaultComPortSettings => throw new NotImplementedException();
}
