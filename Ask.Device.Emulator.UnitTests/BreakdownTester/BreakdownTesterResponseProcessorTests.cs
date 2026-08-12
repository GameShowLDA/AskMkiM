using Ask.Device.ResponseProcessor.BreakdownTester.ResponseProcessing;

namespace Ask.Device.Emulator.UnitTests.BreakdownTester;

public sealed class BreakdownTesterResponseProcessorTests
{
  [Theory]
  [InlineData("1.250kV", 1.25)]
  [InlineData(" 12,5 mA ", 12.5)]
  [InlineData("60Hz", 60)]
  [InlineData("-2.5E-3 A", -0.0025)]
  public void TryParseNumber_WithValidResponse_ReturnsValue(string response, double expected)
  {
    bool success = BreakdownTesterResponseProcessor.TryParseNumber(response, out double value);

    Assert.True(success);
    Assert.Equal(expected, value, 6);
  }

  [Theory]
  [InlineData("")]
  [InlineData("ERROR")]
  [InlineData("ERROR 12")]
  [InlineData("---")]
  public void TryParseNumber_WithInvalidResponse_ReturnsFalse(string response)
  {
    Assert.False(BreakdownTesterResponseProcessor.TryParseNumber(response, out _));
  }

  [Theory]
  [InlineData("ON", true)]
  [InlineData(" off ", false)]
  public void TryParseState_WithValidResponse_ReturnsState(string response, bool expected)
  {
    bool success = BreakdownTesterResponseProcessor.TryParseState(response, out bool state);

    Assert.True(success);
    Assert.Equal(expected, state);
  }

  [Fact]
  public void CheckMode_IgnoresCaseAndWhitespace()
  {
    Assert.True(BreakdownTesterResponseProcessor.CheckMode(" acw ", "ACW"));
  }

  [Fact]
  public void TryParseMeasurement_ExtractsStatusValueAndUnit()
  {
    bool success = BreakdownTesterResponseProcessor.TryParseMeasurement(
      "1,PASS,0.500kV,125.4MOhm",
      out var result);

    Assert.True(success);
    Assert.NotNull(result);
    Assert.Equal("PASS", result.Status);
    Assert.Equal(125.4, result.Value, 6);
    Assert.Equal("MOhm", result.Unit);
  }

  [Fact]
  public void CheckInitialization_ValidatesExpectedDeviceIdentifier()
  {
    Assert.True(BreakdownTesterResponseProcessor.CheckInitialization("GW INSTEK,GPT-79904", "GPT"));
    Assert.False(BreakdownTesterResponseProcessor.CheckInitialization("KEYSIGHT,34465A", "GPT"));
  }

  [Fact]
  public void TestStatusChecks_DistinguishRunningFailureAndStoppedResponses()
  {
    Assert.True(BreakdownTesterResponseProcessor.IsTestInProgress("1,TEST,0.500kV,0.1mA"));
    Assert.True(BreakdownTesterResponseProcessor.IsTestFailed("1,FAIL,0.500kV,1.2mA"));
    Assert.True(BreakdownTesterResponseProcessor.IsTestStopped("TEST OFF"));
    Assert.False(BreakdownTesterResponseProcessor.IsTestInProgress("TEST OFF"));
  }
}
