using Ask.Core.Services.App;
using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.DTO.Settings;

namespace Ask.Engine.UnitTests.Services.Config;

[Collection(nameof(ExecutionConfigCollection))]
public sealed class LegacyStepByStepSettingTests
{
  [Fact]
  public async Task LoadingLegacyEnabledValueDoesNotEnableStepMode()
  {
    var previousSettings = await ExecutionConfig.GetExecitonModel();
    try
    {
      await ExecutionConfig.SetExecutionModel(new SettingsExecutionDto
      {
        StepByStepMode = true,
      });

      Assert.False(StepControlManager.StepMode);
      Assert.False(ExecutionConfig.GetExecutionModelSnapshot().StepByStepMode);
    }
    finally
    {
      await ExecutionConfig.SetExecutionModel(previousSettings);
      StepControlManager.DisableStepMode();
    }
  }
}
