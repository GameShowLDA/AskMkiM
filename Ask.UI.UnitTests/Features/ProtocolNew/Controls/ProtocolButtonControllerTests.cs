using Ask.UI.Features.ProtocolNew.Controls;

namespace Ask.UI.UnitTests.Features.ProtocolNew.Controls;

public sealed class ProtocolButtonControllerTests
{
  [Theory]
  [InlineData(true, ProtocolButtonState.Ready, true)]
  [InlineData(true, ProtocolButtonState.Paused, true)]
  [InlineData(true, ProtocolButtonState.Running, false)]
  [InlineData(true, ProtocolButtonState.AdditionalActions, false)]
  [InlineData(true, ProtocolButtonState.InteractiveActions, false)]
  [InlineData(true, ProtocolButtonState.RetryOrContinue, false)]
  [InlineData(true, ProtocolButtonState.ExitOnly, false)]
  [InlineData(true, ProtocolButtonState.Hidden, false)]
  [InlineData(false, ProtocolButtonState.Ready, false)]
  [InlineData(false, ProtocolButtonState.Paused, false)]
  internal void RuntimeExecutionSettingsAreShownOnlyInReadyOrPausedState(
    bool enabled,
    ProtocolButtonState state,
    bool expected)
  {
    bool actual = ProtocolButtonController.ShouldShowRuntimeExecutionSettings(enabled, state);

    Assert.Equal(expected, actual);
  }
}
