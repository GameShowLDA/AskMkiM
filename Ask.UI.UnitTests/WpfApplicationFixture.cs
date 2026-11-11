using System.Windows;
using System.Windows.Threading;

namespace Ask.UI.UnitTests;

[CollectionDefinition(nameof(WpfApplicationCollection))]
public sealed class WpfApplicationCollection : ICollectionFixture<WpfApplicationFixture>
{
}

public sealed class WpfApplicationFixture : IAsyncLifetime
{
  private static readonly TimeSpan StartupTimeout = TimeSpan.FromSeconds(30);
  private readonly TaskCompletionSource<Application> _application =
    new(TaskCreationOptions.RunContinuationsAsynchronously);

  public Task InitializeAsync()
  {
    if (Application.Current is { } application)
    {
      _application.SetResult(application);
      return Task.CompletedTask;
    }

    var thread = new Thread(RunApplication) { IsBackground = true };
    thread.SetApartmentState(ApartmentState.STA);
    thread.Start();
    return _application.Task.WaitAsync(StartupTimeout);
  }

  public async Task RunAsync(Func<Task> test, TimeSpan timeout)
  {
    var application = await _application.Task;
    await application.Dispatcher.InvokeAsync(test).Task.Unwrap().WaitAsync(timeout);
  }

  public Task DisposeAsync() => Task.CompletedTask;

  private void RunApplication()
  {
    try
    {
      var application = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
      _application.SetResult(application);
      application.Run();
    }
    catch (Exception exception)
    {
      _application.TrySetException(exception);
    }
  }
}
