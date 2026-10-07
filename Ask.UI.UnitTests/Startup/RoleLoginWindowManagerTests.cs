using Ask.Core.Shared.Entity.Settings;
using Ask.Core.Shared.Metadata.Enums.RoleEnums;
using MainWindowProgram.Init;
using System.Windows;
using System.Windows.Threading;

namespace Ask.UI.UnitTests.Startup
{
  public sealed class RoleLoginWindowManagerTests
  {
    [Fact]
    public async Task StartupOpeningReturnsToDispatcherAndContinuesAfterAuthentication()
    {
      var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
      var thread = new Thread(() =>
      {
        var dispatcher = Dispatcher.CurrentDispatcher;
        SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(dispatcher));
        dispatcher.BeginInvoke(new Action(async () =>
        {
          var manager = new RoleLoginWindowManager();
          try
          {
            var opening = manager.ShowAsync();
            Assert.False(opening.IsCompleted);
            await opening;
            Assert.True(dispatcher.CheckAccess());
            var role = await manager.WaitForAuthenticationAsync();
            Assert.NotNull(role);
            Assert.Equal(RoleType.Administrator, role.Role);
            await manager.UpdateLoadingStatusAsync("Инициализация главного окна...");
            await manager.CloseAsync();
            Assert.False(manager.IsClosedByUser);
            Assert.False(RoleLoginWindowManager.IsAuthenticationActive);
            completion.TrySetResult();
          }
          catch (Exception error)
          {
            completion.TrySetException(error);
          }
          finally
          {
            dispatcher.BeginInvokeShutdown(DispatcherPriority.Background);
          }
        }));
        Dispatcher.Run();
      }) { IsBackground = true };
      thread.SetApartmentState(ApartmentState.STA);
      thread.Start();
      await completion.Task.WaitAsync(TimeSpan.FromSeconds(15));
      Assert.True(thread.Join(TimeSpan.FromSeconds(5)));
    }
  }
}

namespace MainWindowProgram
{
  // Заменяет только форму и проверку пароля; manager и анимация подключены из production-кода.
  internal sealed class RoleLoginWindow : Window
  {
    private readonly TaskCompletionSource<RoleCredentialModel?> _authentication = new();

    public RoleLoginWindow(IReadOnlySet<RoleType>? rolesWithSavedSessions)
    {
      Width = 1;
      Height = 1;
      ShowInTaskbar = false;
      ShowActivated = false;
      WindowStyle = WindowStyle.None;
      Loaded += (_, _) => _authentication.TrySetResult(new RoleCredentialModel { Role = RoleType.Administrator });
    }

    public Task<RoleCredentialModel?> WaitForAuthenticationAsync() => _authentication.Task;
    public void UpdateLoadingStatus(string message) => Dispatcher.VerifyAccess();
    public void FailStartupLoading(string message) => Dispatcher.VerifyAccess();
    public void CompleteStartupLoading() => Close();
  }
}
