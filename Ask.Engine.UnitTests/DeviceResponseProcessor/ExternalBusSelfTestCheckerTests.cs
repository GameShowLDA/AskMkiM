using Ask.Device.ResponseProcessor.ModuleRelayControl.ResponseProcessing.Checkers;

namespace Ask.Engine.UnitTests.DeviceResponseProcessor;

public sealed class ExternalBusSelfTestCheckerTests
{
  private const string SuccessfulResponse =
    """
    {
      "ModuleName": "MKR",
      "NumberDevice": 6,
      "NumberChassis": 1,
      "NumberBus": 2,
      "ProtectReleBusA": 103,
      "ProtectReleBusB": 111,
      "ConnectProtect": true,
      "MainReleBusA": 104,
      "MainReleBusB": 112,
      "ConnectMain": true,
      "Error": 0
    }
    """;

  [Fact]
  public void Check_ValidSuccessfulResponse_ReturnsTrue()
  {
    Assert.True(ExternalBusSelfTestChecker.Check(SuccessfulResponse, 1, 6, 2));
  }

  [Theory]
  [InlineData("NumberChassis", "1", "2")]
  [InlineData("NumberDevice", "6", "7")]
  [InlineData("NumberBus", "2", "3")]
  [InlineData("ConnectProtect", "true", "false")]
  [InlineData("ConnectMain", "true", "false")]
  [InlineData("Error", "0", "1")]
  public void Check_MismatchedOrFailedField_ReturnsFalse(
    string field,
    string currentValue,
    string replacementValue)
  {
    string response = SuccessfulResponse.Replace(
      $"\"{field}\": {currentValue}",
      $"\"{field}\": {replacementValue}",
      StringComparison.Ordinal);

    Assert.False(ExternalBusSelfTestChecker.Check(response, 1, 6, 2));
  }

  [Theory]
  [InlineData("ProtectReleBusA")]
  [InlineData("ProtectReleBusB")]
  [InlineData("MainReleBusA")]
  [InlineData("MainReleBusB")]
  public void Check_MissingRelayField_ReturnsFalse(string field)
  {
    string response = string.Join(
      Environment.NewLine,
      SuccessfulResponse
        .Split(Environment.NewLine)
        .Where(line => !line.Contains($"\"{field}\"", StringComparison.Ordinal)));

    Assert.False(ExternalBusSelfTestChecker.Check(response, 1, 6, 2));
  }

  [Fact]
  public void Check_ChangedRelayNumbers_ReturnsTrue()
  {
    string response = SuccessfulResponse
      .Replace("\"ProtectReleBusA\": 103", "\"ProtectReleBusA\": 501", StringComparison.Ordinal)
      .Replace("\"ProtectReleBusB\": 111", "\"ProtectReleBusB\": 502", StringComparison.Ordinal)
      .Replace("\"MainReleBusA\": 104", "\"MainReleBusA\": 503", StringComparison.Ordinal)
      .Replace("\"MainReleBusB\": 112", "\"MainReleBusB\": 504", StringComparison.Ordinal);

    Assert.True(ExternalBusSelfTestChecker.Check(response, 1, 6, 2));
  }

  [Theory]
  [InlineData(0)]
  [InlineData(5)]
  public void Check_UnsupportedBusNumber_ReturnsFalse(int busNumber)
  {
    Assert.False(ExternalBusSelfTestChecker.Check(SuccessfulResponse, 1, 6, busNumber));
  }

  [Theory]
  [InlineData("")]
  [InlineData("not json")]
  public void Check_EmptyOrMalformedResponse_ReturnsFalse(string response)
  {
    Assert.False(ExternalBusSelfTestChecker.Check(response, 1, 6, 2));
  }
}
