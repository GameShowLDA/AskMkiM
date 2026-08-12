using Ask.Device.ResponseProcessor.ModuleRelayControl.ResponseProcessing.Checkers;

namespace Ask.Engine.UnitTests.DeviceResponseProcessor;

public sealed class PointSelfTestCheckerTests
{
  private const string SuccessfulResponse =
    """
    {
      "ModuleName": "MKR",
      "NumberDevice": 6,
      "NumberChassis": 1,
      "Status": "sucsess",
      "NumberPoint": 100,
      "ConnectPoint": true,
      "DisconnectBusA": true,
      "DisconnectBusB": true,
      "SelfControl": true
    }
    """;

  [Fact]
  public void Check_ValidSuccessfulResponse_ReturnsTrue()
  {
    bool result = PointSelfTestChecker.Check(
      SuccessfulResponse,
      chassisNumber: 1,
      moduleNumber: 6,
      pointNumber: 100);

    Assert.True(result);
  }

  [Theory]
  [InlineData("NumberChassis", "1", "2")]
  [InlineData("NumberDevice", "6", "7")]
  [InlineData("NumberPoint", "100", "99")]
  [InlineData("Status", "\"sucsess\"", "\"InvalidParametr\"")]
  [InlineData("ConnectPoint", "true", "false")]
  [InlineData("DisconnectBusA", "true", "false")]
  [InlineData("DisconnectBusB", "true", "false")]
  [InlineData("SelfControl", "true", "false")]
  public void Check_MismatchedOrFailedField_ReturnsFalse(
    string field,
    string currentValue,
    string replacementValue)
  {
    string response = SuccessfulResponse.Replace(
      $"\"{field}\": {currentValue}",
      $"\"{field}\": {replacementValue}",
      StringComparison.Ordinal);

    bool result = PointSelfTestChecker.Check(response, 1, 6, 100);

    Assert.False(result);
  }

  [Theory]
  [InlineData("")]
  [InlineData("not json")]
  public void Check_EmptyOrMalformedResponse_ReturnsFalse(string response)
  {
    Assert.False(PointSelfTestChecker.Check(response, 1, 6, 100));
  }
}
