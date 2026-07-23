using Ask.UI.Controls.ProtocolNew;

namespace Ask.UI.UnitTests.Controls.ProtocolNew;

public sealed class ProtocolUIInteractionPolicyTests
{
  [Theory]
  [InlineData(false)]
  [InlineData(true)]
  public void HardwareErrorAlwaysWaitsForOperator(bool stopOnError)
  {
    bool result = ProtocolUI.ShouldWaitForUserAction(
      stopOnError,
      loop: false,
      deviceTask: true);

    Assert.True(result);
  }

  [Fact]
  public void MeasurementFailureDoesNotWaitWhenStopOnErrorIsDisabled()
  {
    bool result = ProtocolUI.ShouldWaitForUserAction(
      stopOnError: false,
      loop: false,
      deviceTask: false);

    Assert.False(result);
  }

  [Fact]
  public void MeasurementFailureWaitsWhenStopOnErrorIsEnabled()
  {
    bool result = ProtocolUI.ShouldWaitForUserAction(
      stopOnError: true,
      loop: false,
      deviceTask: false);

    Assert.True(result);
  }
}
