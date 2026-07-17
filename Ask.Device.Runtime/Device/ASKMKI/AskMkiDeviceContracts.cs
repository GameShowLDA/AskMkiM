using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Engine.Tests.SelfControl.LegacyAskProtocol;

namespace Ask.Device.Runtime.Device.ASKMKI;

public interface IAskMkiController : IDevice
{
  bool IsIdleMode { get; set; }

  bool UseNetworkProtocol { get; set; }

  byte NetworkAddress { get; set; }

  Task<ushort> ReadRegisterAsync(LegacyAskRegister register, CancellationToken cancellationToken = default);

  Task WriteRegisterAsync(LegacyAskRegister register, ushort value, CancellationToken cancellationToken = default);

  Task WriteSubRegisterAsync(LegacyAskRegister register, byte subRegister, ushort value, CancellationToken cancellationToken = default);

  Task<ushort> ReadAdcAsync(CancellationToken cancellationToken = default);

  Task WriteCommandRegisterAsync(ushort value, CancellationToken cancellationToken = default);

  Task WriteBusCommandAsync(ushort value, CancellationToken cancellationToken = default);

  Task CheckElectronicConnectionAsync(ushort pointAddress, CancellationToken cancellationToken = default);

  Task CheckElectronicDisconnectionAsync(ushort pointAddress, CancellationToken cancellationToken = default);

  Task CheckNoElectronicConnectionAsync(ushort pointAddress, CancellationToken cancellationToken = default);

  Task SetTimerStopAsync(ushort value, CancellationToken cancellationToken = default);

  Task StartTimerAsync(ushort value, CancellationToken cancellationToken = default);

  Task<ushort> ReadTimerReadyAsync(ushort stopFlag, CancellationToken cancellationToken = default);

  Task<ushort> ReadTimerWordAsync(ushort offset, CancellationToken cancellationToken = default);

  Task WriteStrobeCountAsync(ushort count, CancellationToken cancellationToken = default);

  Task SetStrobeAsync(ushort pointAddress, byte parameter, CancellationToken cancellationToken = default);
}

public interface IAskMkiAttachableDevice : IDevice
{
  int NumberChassis { get; set; }
}

public interface IAskMkiAcp : IAskMkiAttachableDevice
{
  Task SetModeAsync(IAskMkiController controller, ushort mode, ushort positiveBus, ushort negativeBus, CancellationToken cancellationToken = default);

  Task<ushort> ReadAsync(IAskMkiController controller, ushort mode, ushort positiveBus, ushort negativeBus, CancellationToken cancellationToken = default);
}

public interface IAskMkiPint : IAskMkiAttachableDevice
{
  int PintNumber { get; }

  Task SetOutputAsync(IAskMkiController controller, double volts, double amps, ushort positiveBus, ushort negativeBus, CancellationToken cancellationToken = default);

  Task SetBusesAsync(IAskMkiController controller, ushort positiveBus, ushort negativeBus, CancellationToken cancellationToken = default);

  Task ResetAsync(IAskMkiController controller, CancellationToken cancellationToken = default);
}

public interface IAskMkiPpu : IAskMkiAttachableDevice
{
  Task SetModeAsync(IAskMkiController controller, int voltage, ushort mode, CancellationToken cancellationToken = default);

  Task StartAsync(IAskMkiController controller, ushort mode, CancellationToken cancellationToken = default);

  Task<ushort> ReadStatusAsync(IAskMkiController controller, CancellationToken cancellationToken = default);

  Task ResetAsync(IAskMkiController controller, CancellationToken cancellationToken = default);
}

public interface IAskMkiPki : IAskMkiAttachableDevice
{
  Task RunMeasurementAsync(IAskMkiController controller, int voltageRange, double resistanceOhm, CancellationToken cancellationToken = default);
}

public interface IAskMkiCommutator : IAskMkiAttachableDevice
{
  Task ConnectVoltmeterAsync(IAskMkiController controller, ushort inputBus, ushort groundBus, CancellationToken cancellationToken = default);

  Task WriteCommandRegisterAsync(IAskMkiController controller, ushort command, CancellationToken cancellationToken = default);

  Task CheckElectronicConnectionAsync(IAskMkiController controller, ushort pointAddress, CancellationToken cancellationToken = default);

  Task CheckElectronicDisconnectionAsync(IAskMkiController controller, ushort pointAddress, CancellationToken cancellationToken = default);

  Task CheckNoElectronicConnectionAsync(IAskMkiController controller, ushort pointAddress, CancellationToken cancellationToken = default);
}

public interface IAskMkiTimer : IAskMkiAttachableDevice
{
  Task SetStopAsync(IAskMkiController controller, ushort value, CancellationToken cancellationToken = default);

  Task StartAsync(IAskMkiController controller, ushort value, CancellationToken cancellationToken = default);

  Task<ushort> ReadReadyAsync(IAskMkiController controller, ushort stopFlag, CancellationToken cancellationToken = default);

  Task<ushort> ReadWordAsync(IAskMkiController controller, ushort offset, CancellationToken cancellationToken = default);
}
