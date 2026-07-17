using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Device.Runtime.Device.ASKMKI;

namespace Ask.Device.Runtime.Device.TIMER;

public sealed class ASKMKI_Timer : AskMkiDeviceBase, IAskMkiTimer
{
  public ASKMKI_Timer()
    : base("АСК: таймер", "Таймер АЦП старого тестера АСК", DeviceType.Unknown)
  {
  }

  public Task SetStopAsync(IAskMkiController controller, ushort value, CancellationToken cancellationToken = default)
  {
    return controller.SetTimerStopAsync(value, cancellationToken);
  }

  public Task StartAsync(IAskMkiController controller, ushort value, CancellationToken cancellationToken = default)
  {
    return controller.StartTimerAsync(value, cancellationToken);
  }

  public Task<ushort> ReadReadyAsync(IAskMkiController controller, ushort stopFlag, CancellationToken cancellationToken = default)
  {
    return controller.ReadTimerReadyAsync(stopFlag, cancellationToken);
  }

  public Task<ushort> ReadWordAsync(IAskMkiController controller, ushort offset, CancellationToken cancellationToken = default)
  {
    return controller.ReadTimerWordAsync(offset, cancellationToken);
  }
}
