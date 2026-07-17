using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Device.Runtime.Device.ASKMKI;
using Ask.Engine.Tests.SelfControl.LegacyAskProtocol;

namespace Ask.Device.Runtime.Device.ASKMKI_ACP;

public sealed class ASKMKI_ACP : AskMkiDeviceBase, IAskMkiAcp
{
  public ASKMKI_ACP()
    : base("АСК: АЦП", "АЦП старого тестера АСК", DeviceType.PrecisionMeter)
  {
  }

  public async Task SetModeAsync(IAskMkiController controller, ushort mode, ushort positiveBus, ushort negativeBus, CancellationToken cancellationToken = default)
  {
    await controller.WriteRegisterAsync(LegacyAskRegister.AcpMode, mode, cancellationToken).ConfigureAwait(false);
    await controller.WriteRegisterAsync(LegacyAskRegister.AcpGate, ToAcpGateWord(positiveBus, negativeBus), cancellationToken).ConfigureAwait(false);
  }

  public async Task<ushort> ReadAsync(IAskMkiController controller, ushort mode, ushort positiveBus, ushort negativeBus, CancellationToken cancellationToken = default)
  {
    await SetModeAsync(controller, mode, positiveBus, negativeBus, cancellationToken).ConfigureAwait(false);
    return await controller.ReadAdcAsync(cancellationToken).ConfigureAwait(false);
  }

  private static ushort ToAcpGateWord(ushort positiveBus, ushort negativeBus)
  {
    ushort word = 0;
    if ((positiveBus & LegacyAskBus.A1) != 0) word |= 0x0001;
    if ((positiveBus & LegacyAskBus.B1) != 0) word |= 0x0002;
    if ((positiveBus & LegacyAskBus.A2) != 0) word |= 0x0004;
    if ((positiveBus & LegacyAskBus.B2) != 0) word |= 0x0008;
    if ((negativeBus & LegacyAskBus.A1) != 0) word |= 0x0010;
    if ((negativeBus & LegacyAskBus.B1) != 0) word |= 0x0020;
    if ((negativeBus & LegacyAskBus.A2) != 0) word |= 0x0040;
    if ((negativeBus & LegacyAskBus.B2) != 0) word |= 0x0080;
    return word;
  }
}
