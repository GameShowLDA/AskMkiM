using Ask.UI.Controls.ProtocolNew;
using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.UI;
using Ask.Core.Shared.DTO.Settings;

namespace Ask.UI.UnitTests.Controls.ProtocolNew;

public sealed class ProtocolUIInteractionPolicyTests
{
  [Fact]
  public async Task SavingRuntimeExecutionSettingsPublishesUpdatedApplicationModel()
  {
    SettingsExecutionDto original = ExecutionConfig.GetExecutionModelSnapshot();
    SettingsExecutionDto? saved = null;
    void Capture(SettingsExecutionDto model) => saved = model;

    try
    {
      ExecutionConfig.ExecutionModelSaved += Capture;
      ExecutionConfig.SetStopOnError(!original.StopOnError);
      ExecutionConfig.SetRepeatMeasurement(!original.RepeatMeasurement);

      await ExecutionConfig.SaveExecutionModel(ExecutionConfig.GetExecutionModelSnapshot());

      Assert.NotNull(saved);
      Assert.Equal(!original.StopOnError, saved.StopOnError);
      Assert.Equal(!original.RepeatMeasurement, saved.RepeatMeasurement);
    }
    finally
    {
      ExecutionConfig.ExecutionModelSaved -= Capture;
      await ExecutionConfig.SetExecutionModel(original);
    }
  }

  [Fact]
  public void RuntimeSettingsChangedDuringCommandAreReadForNextDecision()
  {
    bool originalRepeat = ExecutionConfig.GetIsRepeatMeasurementEnabled();
    bool originalSetting = ExecutionConfig.GetStopOnErrorEnabled();

    try
    {
      ExecutionConfig.SetStopOnError(true);
      ExecutionConfig.SetRepeatMeasurement(true);
      using (ControlProgramCommandExecutionContext.Enter())
      {
        ExecutionConfig.SetStopOnError(false);
        ExecutionConfig.SetRepeatMeasurement(false);
        Assert.False(ControlProgramCommandExecutionContext.GetCurrentStopOnErrorEnabled());
        Assert.True(ControlProgramCommandExecutionContext.RepeatMeasurementEnabled);
        Assert.False(ControlProgramCommandExecutionContext.GetCurrentStopOnErrorEnabled());
      }

      Assert.False(ControlProgramCommandExecutionContext.GetCurrentStopOnErrorEnabled());
      Assert.Null(ControlProgramCommandExecutionContext.RepeatMeasurementEnabled);
    }
    finally
    {
      ExecutionConfig.SetStopOnError(originalSetting);
      ExecutionConfig.SetRepeatMeasurement(originalRepeat);
    }
  }

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
