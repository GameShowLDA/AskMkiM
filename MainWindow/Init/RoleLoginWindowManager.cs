using Ask.Core.Shared.Entity.Settings;
using Ask.Core.Shared.Metadata.Enums.RoleEnums;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;

namespace MainWindowProgram.Init
{
  internal sealed class RoleLoginWindowManager
  {
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnableWindow(IntPtr windowHandle, [MarshalAs(UnmanagedType.Bool)] bool enable);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowEnabled(IntPtr windowHandle);

    private Thread? _windowThread;
    private RoleLoginWindow? _window;
    private readonly TaskCompletionSource<RoleCredentialModel?> _authenticationSource =
      new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource<bool> _windowClosedSource =
      new(TaskCreationOptions.RunContinuationsAsynchronously);

    public void Show(IReadOnlySet<RoleType>? rolesWithSavedSessions = null)
    {
      StartWindow(rolesWithSavedSessions, null).GetAwaiter().GetResult();
    }

    public Task ShowAsync(IReadOnlySet<RoleType>? rolesWithSavedSessions, Window owner)
    {
      return StartWindow(rolesWithSavedSessions, owner);
    }

    private Task StartWindow(IReadOnlySet<RoleType>? rolesWithSavedSessions, Window? owner)
    {
      if (_windowThread != null)
      {
        return Task.CompletedTask;
      }

      var ownerHandle = owner == null ? IntPtr.Zero : new WindowInteropHelper(owner).Handle;
      bool ownerWasEnabled = ownerHandle != IntPtr.Zero && IsWindowEnabled(ownerHandle);
      if (ownerWasEnabled)
      {
        EnableWindow(ownerHandle, false);
      }
      var windowStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

      _windowThread = new Thread(() =>
      {
        var dispatcher = Dispatcher.CurrentDispatcher;
        var loginWindow = new RoleLoginWindow(rolesWithSavedSessions);
        _window = loginWindow;

        loginWindow.Loaded += (_, _) => windowStarted.TrySetResult();
        loginWindow.Closed += (_, _) =>
        {
          _window = null;
          if (ownerWasEnabled)
          {
            EnableWindow(ownerHandle, true);
          }
          _windowClosedSource.TrySetResult(true);
          dispatcher.BeginInvokeShutdown(DispatcherPriority.Background);
        };

        _ = loginWindow.WaitForAuthenticationAsync().ContinueWith(
          task => _authenticationSource.TrySetResult(task.Result),
          TaskScheduler.Default);

        loginWindow.Show();
        Dispatcher.Run();
      });

      _windowThread.SetApartmentState(ApartmentState.STA);
      _windowThread.IsBackground = true;
      _windowThread.Start();

      return windowStarted.Task;
    }

    public Task<RoleCredentialModel?> WaitForAuthenticationAsync()
    {
      return _authenticationSource.Task;
    }

    public Task UpdateLoadingStatusAsync(string message)
    {
      return InvokeWindowAsync(window => window.UpdateLoadingStatus(message));
    }

    public Task FailStartupLoadingAsync(string message)
    {
      return InvokeWindowAsync(window => window.FailStartupLoading(message));
    }

    public async Task CloseAsync()
    {
      await InvokeWindowAsync(window => window.CompleteStartupLoading());
      await _windowClosedSource.Task;
    }

    public Task WaitForCloseAsync()
    {
      return _windowClosedSource.Task;
    }

    private Task InvokeWindowAsync(Action<RoleLoginWindow> action)
    {
      var window = _window;
      if (window == null)
      {
        return Task.CompletedTask;
      }

      return window.Dispatcher.InvokeAsync(() => action(window)).Task;
    }
  }
}
