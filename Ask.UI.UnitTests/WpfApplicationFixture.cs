using System.Windows;
using System.Windows.Threading;

namespace Ask.UI.UnitTests;

[CollectionDefinition(nameof(WpfApplicationCollection))]
public sealed class WpfApplicationCollection : ICollectionFixture<WpfApplicationFixture>
{
}

public sealed class WpfApplicationFixture : IAsyncLifetime
{
  private static readonly TimeSpan StartupAndShutdownTimeout = TimeSpan.FromSeconds(30);
  private readonly TaskCompletionSource<Application> _application =
    new(TaskCreationOptions.RunContinuationsAsynchronously);
  private readonly TaskCompletionSource _stopped = new(TaskCreationOptions.RunContinuationsAsynchronously);

  public Task InitializeAsync()
  {
    var thread = new Thread(RunApplication) { IsBackground = true };
    thread.SetApartmentState(ApartmentState.STA);
    thread.Start();
    return _application.Task.WaitAsync(StartupAndShutdownTimeout);
  }

  public async Task RunAsync(Func<Task> test, TimeSpan timeout)
  {
    var application = await _application.Task;
    await application.Dispatcher.InvokeAsync(test).Task.Unwrap().WaitAsync(timeout);
  }

  public async Task DisposeAsync()
  {
    if (_application.Task.IsCompletedSuccessfully && !_stopped.Task.IsCompleted)
    {
      var application = await _application.Task;
      await application.Dispatcher.InvokeAsync(application.Shutdown).Task.WaitAsync(StartupAndShutdownTimeout);
      await _stopped.Task.WaitAsync(StartupAndShutdownTimeout);
    }
  }

  private void RunApplication()
  {
    try
    {
      var application = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
      _application.SetResult(application);
      application.Run();
      _stopped.SetResult();
    }
    catch (Exception exception)
    {
      _application.TrySetException(exception);
      _stopped.TrySetException(exception);
    }
  }
}
