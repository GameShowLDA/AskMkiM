using Ask.Core.Services.App;
using System.Diagnostics;

namespace Ask.UI.UnitTests.Services.App;

public sealed class InactivityLockPolicyTests
{
  private static long At(double seconds) => (long)(seconds * Stopwatch.Frequency);

  [Theory]
  [InlineData(1)]
  [InlineData(2)]
  [InlineData(5)]
  [InlineData(10)]
  public void LocksOnlyAtConfiguredBoundary(int minutes)
  {
    var policy = new InactivityLockPolicy();
    Assert.False(policy.ShouldLock(At(0), minutes, false, 0));
    Assert.False(policy.ShouldLock(At(minutes * 60 - 0.01), minutes, false, 0));
    Assert.True(policy.ShouldLock(At(minutes * 60), minutes, false, 0));
  }

  [Theory]
  [InlineData(0)]
  [InlineData(-1)]
  [InlineData(3)]
  public void DisabledAndInvalidIntervalsNeverLock(int minutes)
  {
    var policy = new InactivityLockPolicy();
    Assert.False(policy.ShouldLock(At(0), minutes, false, 0));
    Assert.False(policy.ShouldLock(At(100000), minutes, false, 0));
  }

  [Fact]
  public void InputResetsDeadline()
  {
    var policy = new InactivityLockPolicy();
    policy.ShouldLock(At(0), 1, false, 0);
    policy.Reset(At(45));
    Assert.False(policy.ShouldLock(At(60), 1, false, 0));
    Assert.True(policy.ShouldLock(At(105), 1, false, 0));
  }

  [Fact]
  public void BusyProcessIncludingPauseAndFinalizationPreventsLock()
  {
    var policy = new InactivityLockPolicy();
    policy.ShouldLock(At(0), 1, false, 0);
    Assert.False(policy.ShouldLock(At(3600), 1, true, 0));
    Assert.False(policy.ShouldLock(At(3659), 1, false, At(3600)));
    Assert.True(policy.ShouldLock(At(3660), 1, false, At(3600)));
  }

  [Fact]
  public void ShortProcessBetweenTimerTicksRestartsDeadline()
  {
    var policy = new InactivityLockPolicy();
    policy.ShouldLock(At(0), 1, false, 0);
    Assert.False(policy.ShouldLock(At(60), 1, false, At(59.5)));
    Assert.True(policy.ShouldLock(At(119.5), 1, false, At(59.5)));
  }

  [Fact]
  public void ChangingIntervalAndUnlockingStartNewCountdown()
  {
    var policy = new InactivityLockPolicy();
    policy.ShouldLock(At(0), 10, false, 0);
    Assert.False(policy.ShouldLock(At(500), 1, false, 0));
    Assert.True(policy.ShouldLock(At(560), 1, false, 0));
    policy.Reset(At(600));
    Assert.False(policy.ShouldLock(At(650), 1, false, 0));
    Assert.True(policy.ShouldLock(At(660), 1, false, 0));
  }
}
