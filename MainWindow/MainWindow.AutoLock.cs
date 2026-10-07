using Ask.Core.Services.App;
using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Config.Base;
using Ask.UI.Features.ProtocolNew.Execution;
using Ask.UI.Infrastructure.UI.Overlay.Drawer.Runtime;
using MainWindowProgram.Init;
using System.Diagnostics;
using System.Windows.Input;
using System.Windows.Threading;

namespace MainWindowProgram;

public partial class MainWindow
{
  private readonly InactivityLockPolicy _inactivityLock = new();
  private DispatcherTimer? _inactivityTimer;

  private void InitializeAutoLock()
  {
    _inactivityLock.Reset(Stopwatch.GetTimestamp());
    InputManager.Current.PreProcessInput += AutoLock_PreProcessInput;
    _inactivityTimer = new DispatcherTimer(DispatcherPriority.Background, Dispatcher)
    {
      Interval = TimeSpan.FromSeconds(1)
    };
    _inactivityTimer.Tick += AutoLock_Tick;
    _inactivityTimer.Start();
    Closed += (_, _) =>
    {
      _inactivityTimer.Stop();
      _inactivityTimer.Tick -= AutoLock_Tick;
      InputManager.Current.PreProcessInput -= AutoLock_PreProcessInput;
    };
  }

  private void AutoLock_PreProcessInput(object sender, PreProcessInputEventArgs e)
  {
    if (e.StagingItem.Input is MouseEventArgs or KeyEventArgs or TextCompositionEventArgs)
      _inactivityLock.Reset(Stopwatch.GetTimestamp());
  }

  private async void AutoLock_Tick(object? sender, EventArgs e)
  {
    var timestamp = Stopwatch.GetTimestamp();
    var activity = ExecutionRunGuard.GetActivity();
    bool blocked = activity.IsBusy || SystemStateManager.GetIsLocked() ||
      _isUserSwitchInProgress ||
      RoleLoginWindowManager.IsAuthenticationActive || !IsVisible || !IsEnabled ||
      DrawerHostService.Instance.ShouldBlockGlobalInput;
    if (_inactivityLock.ShouldLock(timestamp, UserInterfaceConfig.GetAutoLockMinutes(),
      blocked, activity.LastReleaseTimestamp))
    {
      _inactivityLock.Reset(timestamp);
      await SwitchCurrentUserAsync();
      _inactivityLock.Reset(Stopwatch.GetTimestamp());
    }
  }
}
