using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Device.Runtime.Device.ASKMKI;
using Ask.Engine.Tests.SelfControl.LegacyAskProtocol;

namespace Ask.Device.Runtime.Device.PKI;

public sealed class ASKMKI_PKI : AskMkiDeviceBase, IAskMkiPki
{
  public ASKMKI_PKI()
    : base("АСК: ПКИ", "ПКИ старого тестера АСК", DeviceType.PrecisionMeter)
  {
  }

  public async Task RunMeasurementAsync(IAskMkiController controller, int voltageRange, double resistanceOhm, CancellationToken cancellationToken = default)
  {
    if (controller.UseNetworkProtocol)
    {
      await ExecuteOnPpuPkiAddressAsync(controller, async () =>
      {
        int dU = Math.Clamp(voltageRange, 1, 7);
        int nlev = Math.Clamp((int)Math.Round(resistanceOhm / 1_000_000.0), 1, LegacyAskPpuNetBits.LevelMask);
        ushort modeWord = (ushort)((LegacyAskPpuNetBits.DevicePkiSi << 8) | (dU << 4) | 1);
        ushort levelWord = (ushort)(LegacyAskPpuNetBits.LevelPkiSi | (nlev ^ LegacyAskPpuNetBits.LevelMask));

        await controller.WriteRegisterAsync(LegacyAskRegisters.PpuNetMode, modeWord, cancellationToken).ConfigureAwait(false);
        await controller.WriteRegisterAsync(LegacyAskRegisters.PpuNetLevel, levelWord, cancellationToken).ConfigureAwait(false);
        await controller.WriteRegisterAsync(LegacyAskRegisters.PpuNetCommand, LegacyAskPpuNetBits.CommandPkiStart, cancellationToken).ConfigureAwait(false);
      }).ConfigureAwait(false);
      return;
    }

    ushort commandWord = (ushort)(LegacyAskCommandBits.ElectronicProbe | LegacyAskCommandBits.ElectronicTop | LegacyAskCommandBits.ElectronicBottom);
    await controller.WriteCommandRegisterAsync(commandWord, cancellationToken).ConfigureAwait(false);
    await controller.ReadAdcAsync(cancellationToken).ConfigureAwait(false);
  }

  private static async Task ExecuteOnPpuPkiAddressAsync(IAskMkiController controller, Func<Task> action)
  {
    byte previousAddress = controller.NetworkAddress;
    controller.NetworkAddress = LegacyAskDeviceAddress.PpuPki;
    try
    {
      await action().ConfigureAwait(false);
    }
    finally
    {
      controller.NetworkAddress = previousAddress;
    }
  }
}
