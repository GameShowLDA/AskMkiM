using Ask.Core.Services.Config.AppSettings;

namespace Ask.Device.Emulator.UnitTests;

internal sealed class TestExecutionMode : IDisposable
{
  public TestExecutionMode(bool idleMode)
  {
    ExecutionConfig.SetIdleMode(idleMode);
  }

  public void Dispose()
  {
    ExecutionConfig.SetIdleMode(false);
  }
}
