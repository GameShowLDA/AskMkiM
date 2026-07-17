using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Device.Runtime.Device.ASKMKI;
using Ask.Engine.Tests.SelfControl.LegacyAskProtocol;

namespace Ask.Device.Runtime.Device.RelaySwitchModule;

public sealed class ASKMKI_Commutator : AskMkiDeviceBase, IAskMkiCommutator
{
  public ASKMKI_Commutator()
    : base("АСК: коммутатор", "Коммутация шин и электронных точек старого тестера АСК", DeviceType.SwitchingDevice)
  {
  }

  public async Task ConnectVoltmeterAsync(IAskMkiController controller, ushort inputBus, ushort groundBus, CancellationToken cancellationToken = default)
  {
    await controller.WriteSubRegisterAsync(LegacyAskRegister.V7Gate, 2, groundBus, cancellationToken).ConfigureAwait(false);
    await controller.WriteSubRegisterAsync(LegacyAskRegister.V7Gate, 1, inputBus, cancellationToken).ConfigureAwait(false);
  }

  public Task WriteCommandRegisterAsync(IAskMkiController controller, ushort command, CancellationToken cancellationToken = default)
  {
    return controller.WriteCommandRegisterAsync(command, cancellationToken);
  }

  public Task CheckElectronicConnectionAsync(IAskMkiController controller, ushort pointAddress, CancellationToken cancellationToken = default)
  {
    return controller.CheckElectronicConnectionAsync(pointAddress, cancellationToken);
  }

  public Task CheckElectronicDisconnectionAsync(IAskMkiController controller, ushort pointAddress, CancellationToken cancellationToken = default)
  {
    return controller.CheckElectronicDisconnectionAsync(pointAddress, cancellationToken);
  }

  public Task CheckNoElectronicConnectionAsync(IAskMkiController controller, ushort pointAddress, CancellationToken cancellationToken = default)
  {
    return controller.CheckNoElectronicConnectionAsync(pointAddress, cancellationToken);
  }
}
