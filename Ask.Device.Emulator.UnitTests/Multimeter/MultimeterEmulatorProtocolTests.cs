using Ask.Device.Emulator.Multimeter;

namespace Ask.Device.Emulator.UnitTests.Multimeter;

public sealed class MultimeterEmulatorProtocolTests
{
  [Fact(DisplayName = "Мультиметр: эмулятор возвращает заданный SCPI-ответ")]
  public async Task Query_ReturnsConfiguredResponse()
  {
    using var mode = new TestExecutionMode(idleMode: true);
    var protocol = new MultimeterEmulatorProtocol("+1.00000000E+01");

    string response = await protocol.QueryAsync("READ?");

    Assert.Equal("+1.00000000E+01", response);
  }

  [Fact(DisplayName = "Мультиметр: пустой ответ сохраняется без подмены")]
  public async Task Query_PreservesEmptyResponse()
  {
    using var mode = new TestExecutionMode(idleMode: true);
    var protocol = new MultimeterEmulatorProtocol(string.Empty);

    Assert.Empty(await protocol.QueryAsync("CONF:RES 100,0.001"));
  }

  [Fact(DisplayName = "Мультиметр: отменённая команда выбрасывает отмену операции")]
  public async Task CancelledQuery_ThrowsCancellation()
  {
    using var mode = new TestExecutionMode(idleMode: true);
    var protocol = new MultimeterEmulatorProtocol("RES");
    using var cancellation = new CancellationTokenSource();
    cancellation.Cancel();

    await Assert.ThrowsAsync<OperationCanceledException>(
      () => protocol.QueryAsync("FUNC?", cancellationToken: cancellation.Token));
  }
}


