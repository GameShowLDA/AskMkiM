using Ask.Core.Services.Config.AppSettings;
using Ask.Engine.ControlCommandExecutor.Executors;

namespace Ask.Engine.UnitTests.ControlCommandExecutor.Executors;

public sealed class EhtCommandExecutorTests
{
  [Theory]
  [InlineData(false, true)]
  [InlineData(true, false)]
  public void ShouldValidatePointConnections_DependsOnlyOnIdleMode(
    bool idleModeEnabled,
    bool expected)
  {
    bool originalIdleMode = ExecutionConfig.GetIsIdleModeEnabled();

    try
    {
      ExecutionConfig.SetIdleMode(idleModeEnabled);

      Assert.Equal(expected, EhtCommandExecutor.ShouldValidatePointConnections());
    }
    finally
    {
      ExecutionConfig.SetIdleMode(originalIdleMode);
    }
  }
}
