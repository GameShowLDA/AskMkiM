using Ask.Device.Emulator.BreakdownTester;

namespace Ask.Device.Emulator.UnitTests.BreakdownTester;

public sealed class BreakdownTesterEmulatorProtocolTests
{
  [Fact(DisplayName = "ППУ: идентификация возвращает модель GPT-79904")]
  public async Task Identification_ReturnsModel()
  {
    using var mode = new TestExecutionMode(idleMode: true);
    var protocol = new BreakdownTesterEmulatorProtocol();

    string response = await protocol.QueryAsync("*IDN?");

    Assert.Contains("GPT-79904", response);
  }

  [Theory(DisplayName = "ППУ: установленный параметр возвращается последующим запросом")]
  [InlineData("MANU:EDIT:MODE ACW", "MANU:EDIT:MODE ?", "ACW")]
  [InlineData("MANU:ACW:VOLT 1.250", "MANU:ACW:VOLT?", "1.250")]
  [InlineData("SYST:BUZZ:PSOUND ON", "SYST:BUZZ:PSOUND ?", "ON")]
  public async Task SetAndGet_ReturnsStoredValue(string setCommand, string getCommand, string expected)
  {
    using var mode = new TestExecutionMode(idleMode: true);
    var protocol = new BreakdownTesterEmulatorProtocol();

    Assert.Empty(await protocol.QueryAsync(setCommand));
    Assert.Equal(expected, await protocol.QueryAsync(getCommand));
  }

  [Fact(DisplayName = "ППУ: состояние испытания изменяется командами включения и выключения")]
  public async Task FunctionTest_StoresState()
  {
    using var mode = new TestExecutionMode(idleMode: true);
    var protocol = new BreakdownTesterEmulatorProtocol();

    await protocol.QueryAsync("FUNC:TEST ON");
    Assert.Equal("TEST ON", await protocol.QueryAsync("FUNC:TEST ?"));
    await protocol.QueryAsync("FUNC:TEST OFF");
    Assert.Equal("TEST OFF", await protocol.QueryAsync("FUNC:TEST?"));
  }

  [Fact(DisplayName = "ППУ: запрос измерения возвращает результат в формате прибора")]
  public async Task Measure_ReturnsInstrumentResponse()
  {
    using var mode = new TestExecutionMode(idleMode: true);
    var protocol = new BreakdownTesterEmulatorProtocol();

    Assert.Equal("PASS,0,0,1.000mA", await protocol.QueryAsync("MEAS ?"));
  }

  [Fact(DisplayName = "ППУ: неизвестный параметр запроса возвращает нулевое значение")]
  public async Task UnknownQuery_ReturnsZero()
  {
    using var mode = new TestExecutionMode(idleMode: true);
    var protocol = new BreakdownTesterEmulatorProtocol();

    Assert.Equal("0", await protocol.QueryAsync("MANU:IR:UNKNOWN ?"));
  }

  [Fact(DisplayName = "ППУ: отменённая команда выбрасывает отмену операции")]
  public async Task CancelledQuery_ThrowsCancellation()
  {
    using var mode = new TestExecutionMode(idleMode: true);
    var protocol = new BreakdownTesterEmulatorProtocol();
    using var cancellation = new CancellationTokenSource();
    cancellation.Cancel();

    await Assert.ThrowsAsync<OperationCanceledException>(
      () => protocol.QueryAsync("*IDN?", cancellationToken: cancellation.Token));
  }
}
