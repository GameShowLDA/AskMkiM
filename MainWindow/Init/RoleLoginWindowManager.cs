using Ask.Core.Shared.Entity.Settings;
using System.Windows.Threading;

namespace MainWindowProgram.Init
{
  internal sealed class RoleLoginWindowManager
  {
    private Thread? _windowThread;
    private RoleLoginWindow? _window;
    private readonly TaskCompletionSource<RoleCredentialModel?> _authenticationSource =
      new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource<bool> _windowClosedSource =
      new(TaskCreationOptions.RunContinuationsAsynchronously);

    public void Show()
    {
      if (_windowThread != null)
      {
        return;
      }

      var windowStarted = new ManualResetEvent(false);

      _windowThread = new Thread(() =>
      {
        var dispatcher = Dispatcher.CurrentDispatcher;
        var loginWindow = new RoleLoginWindow();
        _window = loginWindow;

        loginWindow.Loaded += (_, _) => windowStarted.Set();
        loginWindow.Closed += (_, _) =>
        {
          _window = null;
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

      windowStarted.WaitOne();
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
