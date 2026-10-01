using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.DTO.Executor;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.FileEnums;
using Ask.UI.Controls.ProtocolNew;
using Ask.UI.Features.ProtocolNew.Execution;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace Ask.UI.UnitTests.Features.ProtocolNew.Execution;

public sealed class ExecutionRestartTests
{
  [Fact]
  public async Task RejectedStartsPreserveProtocolAndReadyIsShownOnlyAfterFinalization()
  {
    var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    var thread = new Thread(() =>
    {
      var application = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
      application.Resources.MergedDictionaries.Add(new ResourceDictionary
      {
        Source = new Uri("/UI;component/Style.xaml", UriKind.Relative)
      });
      SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext());
      application.Dispatcher.InvokeAsync(async () =>
      {
        try
        {
          try { await VerifyRestartsAsync(); }
          finally { application.Resources.MergedDictionaries.Clear(); }
          completion.TrySetResult();
        }
        catch (Exception ex) { completion.TrySetException(ex); }
      });
      Dispatcher.Run();
    }) { IsBackground = true };
    thread.SetApartmentState(ApartmentState.STA);
    thread.Start();
    await completion.Task.WaitAsync(TimeSpan.FromSeconds(60));
  }

  private static async Task VerifyRestartsAsync()
  {
    var previousExecution = ExecutionConfig.GetExecutionModelSnapshot();
    var previousPrint = ProtocolConfig.GetPrintProtocol();
    ExecutionConfig.SetIdleMode(true);
    ExecutionConfig.SetStepByStepMode(false);
    ProtocolConfig.SetPrintProtocol(false);
    try
    {
      await VerifyRejectedStartsAsync();
      foreach (var outcome in new[] { "success", "exception", "cancel", "stop", "finalizationException" })
      {
        var protocol = new ProtocolUI { Header = "Restart regression " + outcome };
        var buttons = (IProtocolButtonView)protocol;
        var preparationEntered = NewSignal();
        var continuePreparation = NewSignal();
        var executionEntered = NewSignal();
        var continueExecution = NewSignal();
        var finalizationEntered = NewSignal();
        var continueFinalization = NewSignal();
        var runCount = 0;
        var settings = new ActionSettings
        {
          CheckType = CheckType.SelfTest,
          CheckPower = false,
          PreActionDelegate = async _ =>
          {
            preparationEntered.TrySetResult();
            await continuePreparation.Task;
          },
          StartDelegate = async (_, messages, _, _, token) =>
          {
            Interlocked.Increment(ref runCount);
            await messages.ShowMessageAsync(new ShowMessageModel("current run"), skipPause: true,
              SkipStepModeCheck: true, ignoreOutputValidation: true);
            executionEntered.TrySetResult();
            await continueExecution.Task.WaitAsync(token);
            if (outcome == "exception") throw new InvalidOperationException("regression failure");
            if (outcome == "cancel") throw new OperationCanceledException();
          },
          StopDelegate = async _ =>
          {
            finalizationEntered.TrySetResult();
            await continueFinalization.Task;
            if (outcome == "finalizationException") throw new InvalidOperationException("cleanup failure");
          }
        };
        protocol.SetSettings(settings);
        var run = protocol.StartAsync();
        await preparationEntered.Task.WaitAsync(TimeSpan.FromSeconds(10));
        await protocol.StartAsync();
        ClickStart(protocol);
        Assert.Equal(0, runCount);
        Assert.Equal(Visibility.Collapsed, buttons.StartVisibility);
        continuePreparation.TrySetResult();
        await executionEntered.Task.WaitAsync(TimeSpan.FromSeconds(10));
        var snapshot = protocol.GetShowMessageModels().ToArray();
        await protocol.StartAsync();
        ClickStart(protocol);
        Assert.Equal(1, runCount);
        Assert.Equal(snapshot, protocol.GetShowMessageModels());
        Assert.Equal(Visibility.Collapsed, buttons.StartVisibility);

        Task? stopping = null;
        if (outcome == "stop") stopping = protocol.AbortExecution();
        else continueExecution.TrySetResult();
        await finalizationEntered.Task.WaitAsync(TimeSpan.FromSeconds(10));
        await protocol.StartAsync();
        ClickStart(protocol);
        Assert.Equal(1, runCount);
        Assert.Equal(Visibility.Collapsed, buttons.StartVisibility);
        Assert.False(ExecutionRunGuard.CanHandleInput(new object()));

        bool completionObserved = false;
        bool readyTooEarly = false;
        void OnProcessingChanged(bool active)
        {
          if (active) return;
          completionObserved = true;
          readyTooEarly = buttons.StartVisibility == Visibility.Visible
            || ExecutionRunGuard.CanHandleInput(new object());
        }
        ActionExecutor.StartProcessing += OnProcessingChanged;
        try
        {
          continueFinalization.TrySetResult();
          await run.WaitAsync(TimeSpan.FromSeconds(10));
          if (stopping != null) await stopping.WaitAsync(TimeSpan.FromSeconds(10));
        }
        finally { ActionExecutor.StartProcessing -= OnProcessingChanged; }
        Assert.True(completionObserved);
        Assert.False(readyTooEarly);
        Assert.Equal(Visibility.Visible, buttons.StartVisibility);
        Assert.True(ExecutionRunGuard.CanHandleInput(new object()));

        continueExecution.TrySetResult();
        var oldMessages = protocol.GetShowMessageModels().ToArray();
        await protocol.StartAsync().WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal(2, runCount);
        Assert.DoesNotContain(protocol.GetShowMessageModels(), oldMessages.Contains);
        Assert.Equal(Visibility.Visible, buttons.StartVisibility);
      }
    }
    finally
    {
      await ExecutionConfig.SetExecutionModel(previousExecution);
      ProtocolConfig.SetPrintProtocol(previousPrint);
    }
  }

  private static async Task VerifyRejectedStartsAsync()
  {
    var protocol = new ProtocolUI { Header = "Rejected restart" };
    var buttons = (IProtocolButtonView)protocol;
    int executions = 0;
    protocol.SetSettings(new ActionSettings
    {
      CheckType = CheckType.SelfTest,
      StartDelegate = (_, _, _, _, _) =>
      {
        executions++;
        return Task.CompletedTask;
      }
    });
    var oldMessage = new ShowMessageModel("previous protocol");
    await protocol.ShowMessageAsync(oldMessage, skipPause: true, SkipStepModeCheck: true, ignoreOutputValidation: true);
    var guard = new ExecutionRunGuard();
    var otherOwner = new object();
    Assert.True(guard.TryAcquire("other execution", otherOwner, out _));
    try
    {
      ClickStart(protocol);
      await protocol.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
      Assert.Equal(0, executions);
      Assert.Contains(oldMessage, protocol.GetShowMessageModels());
      Assert.Equal(Visibility.Visible, buttons.StartVisibility);
      Assert.False(ExecutionRunGuard.CanHandleInput(new object()));
    }
    finally { guard.Release(otherOwner); }

    var previousPower = SystemStateManager.GetIsActivePower();
    var previousDisablePowerCheck = ExecutionConfig.GetIsPowerCheckDisabled();
    try
    {
      ExecutionConfig.SetIdleMode(false);
      ExecutionConfig.SetDisablePowerCheck(false);
      SystemStateManager.SetIsActivePower(false);
      ClickStart(protocol);
      await protocol.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
      Assert.Equal(0, executions);
      Assert.Contains(oldMessage, protocol.GetShowMessageModels());
      Assert.Equal(Visibility.Visible, buttons.StartVisibility);
    }
    finally
    {
      SystemStateManager.SetIsActivePower(previousPower);
      ExecutionConfig.SetDisablePowerCheck(previousDisablePowerCheck);
      ExecutionConfig.SetIdleMode(true);
    }
  }

  private static TaskCompletionSource NewSignal() => new(TaskCreationOptions.RunContinuationsAsynchronously);

  private static void ClickStart(ProtocolUI protocol)
  {
    var button = (UIElement)protocol.FindName("StartButton");
    button.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left)
    {
      RoutedEvent = Mouse.PreviewMouseDownEvent
    });
  }
}
