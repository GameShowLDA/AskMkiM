using Ask.Core.Services.App;
using Ask.Core.Shared.Interfaces.ExecutionInterfaces;
using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.EventCore.Adapters;
using Ask.Device.Runtime.Ethernet.Udp.Broadcast;
using System.Windows;

namespace Ask.UI.Features.ProtocolNew.Services;

/// <summary>
/// Выполняет действующую последовательность общего сброса устройств и системного состояния.
/// </summary>
internal sealed class ExecutionSystemResetService : IExecutionSystemResetService
{
  /// <inheritdoc />
  public async Task ResetAsync()
  {
    await Application.Current.Dispatcher.Invoke(async () =>
    {
      await UdpBroadcastCommandSender.ResetAllDevicesAsync();
      SystemStateManager.SetIsLocked(false);

      if (ProtocolConfig.GetTimeStart())
      {
        SystemStateManager._stopwatch.Stop();
      }

      MessageEventAdapter.RaiseInfoMessage(string.Empty);
    });
  }
}
