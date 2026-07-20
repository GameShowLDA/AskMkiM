using Ask.Core.Shared.DTO.Devices.Base;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Device.Runtime.Device.ASKMKI;
using Ask.Engine.Tests.SelfControl.LegacyAskProtocol;

namespace Ask.Device.Runtime.Device.Breakdowntester;

public sealed class ASKMKI_PPU : AskMkiDeviceBase, IAskMkiPpu
{
  public override ComPortSettings DefaultComPortSettings => throw new NotImplementedException();

  public ASKMKI_PPU()
    : base("АСК: ППУ", "ППУ старого тестера АСК", DeviceType.BreakdownTester)
  {
  }

  public async Task SetModeAsync(IAskMkiController controller, int voltage, ushort mode, CancellationToken cancellationToken = default)
  {
    int safeVoltage = Math.Clamp(voltage, 0, 999);
    ushort voltageCode = LegacyAskPpuVoltageCode.FromVoltage(safeVoltage);

    if (controller.UseNetworkProtocol)
    {
      await ExecuteOnPpuPkiAddressAsync(controller, async () =>
      {
        ushort modeWord = (ushort)(LegacyAskPpuNetBits.DevicePpu << 8);
        if ((mode & LegacyAskPpuMode.OneMinute) != 0)
        {
          modeWord |= LegacyAskPpuNetBits.ModeOneMinute;
        }

        ushort levelWord = LegacyAskPpuNetBits.LevelPpu;
        if ((mode & LegacyAskPpuMode.OneSecond) != 0)
        {
          levelWord |= LegacyAskPpuNetBits.LevelOneSecond;
        }

        await controller.WriteRegisterAsync(LegacyAskRegisters.PpuNetVoltage, voltageCode, cancellationToken).ConfigureAwait(false);
        await controller.WriteRegisterAsync(LegacyAskRegisters.PpuNetMode, modeWord, cancellationToken).ConfigureAwait(false);
        await controller.WriteRegisterAsync(LegacyAskRegisters.PpuNetLevel, levelWord, cancellationToken).ConfigureAwait(false);
      }).ConfigureAwait(false);
      return;
    }

    ushort range = safeVoltage <= 125 ? LegacyAskPpuMkiBits.LowRange : LegacyAskPpuMkiBits.MiddleRange;
    ushort modeWordMki = (ushort)(voltageCode | range);
    ushort commandWord = ToMkiPpuMode(mode);

    await controller.WriteRegisterAsync(LegacyAskRegisters.PpuMkiMode, modeWordMki, cancellationToken).ConfigureAwait(false);
    await controller.WriteRegisterAsync(LegacyAskRegisters.PpuMkiCommand, commandWord, cancellationToken).ConfigureAwait(false);
  }

  public Task StartAsync(IAskMkiController controller, ushort mode, CancellationToken cancellationToken = default)
  {
    return controller.UseNetworkProtocol
      ? ExecuteOnPpuPkiAddressAsync(controller, () => controller.WriteRegisterAsync(LegacyAskRegisters.PpuNetCommand, LegacyAskPpuNetBits.CommandPpuStart, cancellationToken))
      : controller.WriteRegisterAsync(LegacyAskRegisters.PpuMkiCommand, (ushort)(ToMkiPpuMode(mode) | LegacyAskPpuMkiBits.Led | LegacyAskPpuMkiBits.Start), cancellationToken);
  }

  public Task<ushort> ReadStatusAsync(IAskMkiController controller, CancellationToken cancellationToken = default)
  {
    return controller.UseNetworkProtocol
      ? ExecuteOnPpuPkiAddressAsync(controller, () => controller.ReadRegisterAsync(LegacyAskRegisters.PpuNetCommand, cancellationToken))
      : controller.ReadRegisterAsync(LegacyAskRegisters.PpuMkiCommand, cancellationToken);
  }

  public async Task ResetAsync(IAskMkiController controller, CancellationToken cancellationToken = default)
  {
    if (controller.UseNetworkProtocol)
    {
      await ExecuteOnPpuPkiAddressAsync(controller, async () =>
      {
        await controller.WriteRegisterAsync(LegacyAskRegisters.PpuNetCommand, LegacyAskPpuNetBits.CommandPpuReset, cancellationToken).ConfigureAwait(false);
        await controller.WriteRegisterAsync(LegacyAskRegisters.PpuNetVoltage, 0, cancellationToken).ConfigureAwait(false);
        await controller.WriteRegisterAsync(LegacyAskRegisters.PpuNetMode, 0, cancellationToken).ConfigureAwait(false);
        await controller.WriteRegisterAsync(LegacyAskRegisters.PpuNetLevel, 0, cancellationToken).ConfigureAwait(false);
      }).ConfigureAwait(false);
      return;
    }

    await controller.WriteRegisterAsync(LegacyAskRegisters.PpuMkiCommand, LegacyAskPpuMkiBits.Reset, cancellationToken).ConfigureAwait(false);
    await controller.WriteRegisterAsync(LegacyAskRegisters.PpuMkiMode, 0, cancellationToken).ConfigureAwait(false);
  }

  private static ushort ToMkiPpuMode(ushort mode)
  {
    ushort result = 0;
    if ((mode & LegacyAskPpuMode.OneMinute) != 0)
    {
      result |= LegacyAskPpuMkiBits.OneMinute;
    }
    else if ((mode & LegacyAskPpuMode.OneSecond) != 0)
    {
      result |= LegacyAskPpuMkiBits.OneSecond;
    }

    if ((mode & LegacyAskPpuMode.MeasureVoltage) != 0)
    {
      result |= LegacyAskPpuMkiBits.MeasureVoltage;
    }

    return result;
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

  private static async Task<T> ExecuteOnPpuPkiAddressAsync<T>(IAskMkiController controller, Func<Task<T>> action)
  {
    byte previousAddress = controller.NetworkAddress;
    controller.NetworkAddress = LegacyAskDeviceAddress.PpuPki;
    try
    {
      return await action().ConfigureAwait(false);
    }
    finally
    {
      controller.NetworkAddress = previousAddress;
    }
  }
}
