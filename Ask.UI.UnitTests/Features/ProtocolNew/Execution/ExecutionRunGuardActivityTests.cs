using Ask.UI.Features.ProtocolNew.Execution;
using System.Diagnostics;

namespace Ask.UI.UnitTests.Features.ProtocolNew.Execution;

public sealed class ExecutionRunGuardActivityTests
{
  [Fact]
  public void ActivityIsHeldUntilOwnerReleasesAndRecordsCompletionTime()
  {
    var guard = new ExecutionRunGuard();
    var owner = new object();
    Assert.True(guard.TryAcquire("test", owner, out _));
    try
    {
      Assert.True(ExecutionRunGuard.GetActivity().IsBusy);
      guard.Release(new object());
      Assert.True(ExecutionRunGuard.GetActivity().IsBusy);
      Assert.False(new ExecutionRunGuard().TryAcquire("other", new object(), out _));
    }
    finally
    {
      var beforeRelease = Stopwatch.GetTimestamp();
      guard.Release(owner);
      var activity = ExecutionRunGuard.GetActivity();
      Assert.False(activity.IsBusy);
      Assert.InRange(activity.LastReleaseTimestamp, beforeRelease, Stopwatch.GetTimestamp());
    }
  }
}
