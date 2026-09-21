using Ask.UI.Features.ProtocolNew.Execution;
using Ask.UI.Features.ProtocolNew.Hotkeys;

namespace Ask.UI.UnitTests.Features.ProtocolNew.Execution;

public sealed class ExecutionCancellationTests
{
  [Fact]
  public async Task CancellationEscapesPausedExecutor()
  {
    var executor = new ActionExecutor();
    using var cancellation = new CancellationTokenSource();
    Assert.True(executor.RequestPause());
    var waiting = executor.WaitAtExecutionCheckpointAsync(cancellation.Token, null!, "test");
    Assert.False(waiting.IsCompleted);
    cancellation.Cancel();
    await Assert.ThrowsAnyAsync<OperationCanceledException>(
      () => waiting.WaitAsync(TimeSpan.FromSeconds(1)));
  }

  [Fact]
  public async Task CancelledCheckpointCannotContinueWhenNotPaused()
  {
    var executor = new ActionExecutor();
    await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
      executor.WaitAtExecutionCheckpointAsync(new CancellationToken(true), null!, "test"));
  }

  [Fact]
  public async Task StepWaitIsCancelledAndNextWaitRemainsIndependent()
  {
    using var cancellation = new CancellationTokenSource();
    var first = KeyboardManager.WaitForNextStepKeyAsync(cancellation.Token);
    cancellation.Cancel();
    await Assert.ThrowsAnyAsync<OperationCanceledException>(() => first.WaitAsync(TimeSpan.FromSeconds(1)));
    var second = KeyboardManager.WaitForNextStepKeyAsync(CancellationToken.None);
    Assert.False(second.IsCompleted);
    KeyboardManager.TriggerStep();
    await second.WaitAsync(TimeSpan.FromSeconds(1));
  }

  [Fact]
  public void ActiveOwnerCannotReenterAndOtherOwnerCannotReceiveInput()
  {
    var guard = new ExecutionRunGuard();
    var owner = new object();
    try
    {
      Assert.True(guard.TryAcquire("test", owner, out _));
      Assert.False(guard.TryAcquire("test", owner, out _));
      Assert.True(ExecutionRunGuard.CanHandleInput(owner));
      Assert.False(ExecutionRunGuard.CanHandleInput(new object()));
    }
    finally { guard.Release(owner); }
    Assert.True(ExecutionRunGuard.CanHandleInput(new object()));
  }
}
