using Ask.UI.Controls.ProtocolNew;

namespace Ask.UI.UnitTests.Controls.ProtocolNew;

public sealed class ProtocolUIPowerStartGuardTests
{
  [Theory]
  [InlineData(false, false, true, false, true)]
  [InlineData(false, true, true, false, false)]
  [InlineData(true, false, true, false, false)]
  [InlineData(false, false, false, false, false)]
  [InlineData(false, false, true, true, false)]
  public void ShouldBlockStartForMissingPower_ReturnsExpectedResult(
    bool isIdleMode,
    bool isPowerActive,
    bool checkPower,
    bool isPowerCheckDisabled,
    bool expected)
  {
    bool result = ProtocolUI.ShouldBlockStartForMissingPower(
      isIdleMode,
      isPowerActive,
      checkPower,
      isPowerCheckDisabled);

    Assert.Equal(expected, result);
  }
}
