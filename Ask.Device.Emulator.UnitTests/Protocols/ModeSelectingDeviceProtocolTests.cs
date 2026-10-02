using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Device.Emulator.Protocols;

namespace Ask.Device.Emulator.UnitTests.Protocols;

public sealed class ModeSelectingDeviceProtocolTests
{
  [Fact]
  public async Task RealWatchdogDeadline_HasDeviceIdentityAndNoResponseKind()
  {
    using var mode = new TestExecutionMode(idleMode: false);
    var device = new Ask.Device.Runtime.Device.KeysightDevice { NumberChassis = 1, Number = 16 };
    var protocol = new ModeSelectingDeviceProtocol(() => new HangingProtocol(),
      new StubProtocol("IDLE"), TimeSpan.FromMilliseconds(50), device);

    var error = await Assert.ThrowsAsync<Ask.Core.Services.Errors.Device.DeviceNoResponseException>(
      () => protocol.QueryAsync("READ?", timeout: 1000));

    Assert.Contains("1.16", error.Message);
    Assert.Contains("READ?", error.Message);
  }

  private sealed class HangingProtocol : IDeviceProtocol
  {
    public SemaphoreSlim OperationLock { get; set; } = new(1, 1);

    public async Task<string> QueryAsync(string command, double responseDelay = 0, int timeout = 0,
      int port = 0, int delayBeforeCall = 0, CancellationToken cancellationToken = default)
    {
      await Task.Delay(Timeout.Infinite, cancellationToken);
      return string.Empty;
    }
  }

  [Fact(DisplayName = "Выбор протокола: рабочий режим вызывает реальное устройство")]
  public async Task RealMode_UsesRealProtocol()
  {
    using var mode = new TestExecutionMode(idleMode: false);
    var real = new StubProtocol("REAL");
    var idle = new StubProtocol("IDLE");
    var protocol = new ModeSelectingDeviceProtocol(() => real, idle);

    string response = await protocol.QueryAsync("COMMAND");

    Assert.Equal("REAL", response);
    Assert.Equal("COMMAND", real.LastCommand);
    Assert.Null(idle.LastCommand);
  }

  [Fact(DisplayName = "Выбор протокола: холостой режим вызывает эмулятор")]
  public async Task IdleMode_UsesEmulatorProtocol()
  {
    using var mode = new TestExecutionMode(idleMode: true);
    var real = new StubProtocol("REAL");
    var idle = new StubProtocol("IDLE");
    var protocol = new ModeSelectingDeviceProtocol(() => real, idle);

    string response = await protocol.QueryAsync("COMMAND");

    Assert.Equal("IDLE", response);
    Assert.Equal("COMMAND", idle.LastCommand);
    Assert.Null(real.LastCommand);
  }

  [Fact(DisplayName = "Выбор протокола: отсутствие реального протокола даёт понятную ошибку")]
  public async Task MissingRealProtocol_ThrowsInvalidOperation()
  {
    using var mode = new TestExecutionMode(idleMode: false);
    var protocol = new ModeSelectingDeviceProtocol(() => null, new StubProtocol("IDLE"));

    var exception = await Assert.ThrowsAsync<InvalidOperationException>(
      () => protocol.QueryAsync("COMMAND"));

    Assert.Contains("не инициализирован", exception.Message);
  }

  private sealed class StubProtocol(string response) : IDeviceProtocol
  {
    public string? LastCommand { get; private set; }
    public SemaphoreSlim OperationLock { get; set; } = new(1, 1);

    public Task<string> QueryAsync(
      string command,
      double responseDelay = 0,
      int timeout = 0,
      int port = 0,
      int delayBeforeCall = 0,
      CancellationToken cancellationToken = default)
    {
      LastCommand = command;
      return Task.FromResult(response);
    }
  }
}
