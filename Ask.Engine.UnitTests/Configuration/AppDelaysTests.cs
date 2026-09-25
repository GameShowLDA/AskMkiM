using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.DTO.Settings;
using Ask.Core.Shared.Metadata.Static.Delays;

namespace Ask.Engine.UnitTests.Configuration;

public sealed class AppDelaysTests
{
  [Fact]
  public void ModuleRelayControlDelays_DefinesDefaultCommandDelays()
  {
    var delays = new ModuleRelayControlDelays();

    Assert.Equal("Задержка перед отправкой команды МКР", delays.PreCommandDelay.Name);
    Assert.Equal(20, delays.PreCommandDelay.Delay);
    Assert.Equal("Задержка после отправки команды МКР", delays.PostCommandDelay.Name);
    Assert.Equal(20, delays.PostCommandDelay.Delay);
  }

  [Fact]
  public void SaveAndApply_UpdatesYamlAndCurrentProcess()
  {
    DelaySettings originalSettings = AppDelays.GetSettings();
    var updatedSettings = new DelaySettings
    {
      BreakdownTester = new BreakdownTesterDelaySettings { PostTestDelay = 140 },
      ModuleRelayControl = new ModuleRelayControlDelaySettings
      {
        PreCommandDelay = 30,
        PostCommandDelay = 40,
      },
    };

    try
    {
      AppDelays.SaveAndApply(updatedSettings);

      DelaySettings currentSettings = AppDelays.GetSettings();
      DelaySettings persistedSettings = new DelaySettingsFileService().Load();
      Assert.Equal(140, currentSettings.BreakdownTester.PostTestDelay);
      Assert.Equal(30, currentSettings.ModuleRelayControl.PreCommandDelay);
      Assert.Equal(40, currentSettings.ModuleRelayControl.PostCommandDelay);
      Assert.Equal(140, persistedSettings.BreakdownTester.PostTestDelay);
      Assert.Equal(30, persistedSettings.ModuleRelayControl.PreCommandDelay);
      Assert.Equal(40, persistedSettings.ModuleRelayControl.PostCommandDelay);
    }
    finally
    {
      AppDelays.SaveAndApply(originalSettings);
    }
  }
}
