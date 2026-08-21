using Ask.Core.Services.App;

namespace Ask.UI.UnitTests.Services.App;

public sealed class StepControlManagerTests : IDisposable
{
  public StepControlManagerTests()
  {
    ClearState();
  }

  [Fact(DisplayName = "Включение шага поверх активирует режим F10")]
  public void EnableStepMode_WhenStepOverRequested_EnablesF10Mode()
  {
    StepControlManager.EnableStepMode(isStepInto: false);

    Assert.True(StepControlManager.StepMode);
    Assert.False(StepControlManager.IsStepInto);
    Assert.True(StepControlManager.StepOverUntilNextControlCommand);
    Assert.Equal(StepModeActivationSource.ManualOrConfig, StepControlManager.ActivationSource);
    Assert.False(StepControlManager.StepBypassRequested);
  }

  [Fact(DisplayName = "Переход из F10 в шаг внутрь активирует режим F11")]
  public void SetStepIntoMode_WhenF10ModeIsActive_EnablesF11Mode()
  {
    StepControlManager.EnableStepMode(isStepInto: false);

    StepControlManager.SetStepIntoMode();

    Assert.True(StepControlManager.StepMode);
    Assert.True(StepControlManager.IsStepInto);
    Assert.False(StepControlManager.StepOverUntilNextControlCommand);
  }

  [Fact(DisplayName = "Отключение пошагового режима очищает состояние и запрашивает пропуск")]
  public void DisableStepMode_WhenStepModeIsActive_ClearsStateAndRequestsBypass()
  {
    StepControlManager.EnableStepMode(isStepInto: true);

    StepControlManager.DisableStepMode();

    Assert.False(StepControlManager.StepMode);
    Assert.True(StepControlManager.StepBypassRequested);
    Assert.False(StepControlManager.StepOverUntilNextControlCommand);
    Assert.Equal(StepModeActivationSource.Unknown, StepControlManager.ActivationSource);
  }

  public void Dispose()
  {
    ClearState();
  }

  private static void ClearState()
  {
    StepControlManager.DisableStepMode();
    StepControlManager.Reset();
  }
}
