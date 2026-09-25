using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.App;
using Ask.Core.Shared.DTO.Settings;

namespace Ask.Engine.UnitTests.Configuration;

public sealed class DelaySettingsFileServiceTests
{
  [Fact]
  public void Load_EmptyFile_WritesAndReturnsDefaults()
  {
    string applicationDirectory = CreateApplicationDirectory();
    string settingsPath = GetSettingsPath(applicationDirectory);

    try
    {
      Directory.CreateDirectory(Path.GetDirectoryName(settingsPath)!);
      File.WriteAllText(settingsPath, string.Empty);

      DelaySettings settings = new DelaySettingsFileService(applicationDirectory).Load();

      Assert.Equal(100, settings.BreakdownTester.PostTestDelay);
      Assert.Equal(20, settings.ModuleRelayControl.PreCommandDelay);
      Assert.Equal(20, settings.ModuleRelayControl.PostCommandDelay);
      Assert.False(string.IsNullOrWhiteSpace(File.ReadAllText(settingsPath)));
    }
    finally
    {
      Directory.Delete(applicationDirectory, recursive: true);
    }
  }

  [Fact]
  public void Load_ExistingFile_ReturnsConfiguredValues()
  {
    string applicationDirectory = CreateApplicationDirectory();
    string settingsPath = GetSettingsPath(applicationDirectory);

    try
    {
      var configuredSettings = new DelaySettings
      {
        BreakdownTester = new BreakdownTesterDelaySettings
        {
          PostTestDelay = 125,
        },
        ModuleRelayControl = new ModuleRelayControlDelaySettings
        {
          PreCommandDelay = 30,
          PostCommandDelay = 40,
        },
      };
      new YamlService<DelaySettings>(settingsPath).Save(configuredSettings);

      DelaySettings settings = new DelaySettingsFileService(applicationDirectory).Load();

      Assert.Equal(125, settings.BreakdownTester.PostTestDelay);
      Assert.Equal(30, settings.ModuleRelayControl.PreCommandDelay);
      Assert.Equal(40, settings.ModuleRelayControl.PostCommandDelay);
    }
    finally
    {
      Directory.Delete(applicationDirectory, recursive: true);
    }
  }

  [Fact]
  public void Save_PersistsSpecifiedValues()
  {
    string applicationDirectory = CreateApplicationDirectory();
    string settingsPath = GetSettingsPath(applicationDirectory);

    try
    {
      var settings = new DelaySettings
      {
        BreakdownTester = new BreakdownTesterDelaySettings { PostTestDelay = 150 },
        ModuleRelayControl = new ModuleRelayControlDelaySettings
        {
          PreCommandDelay = 25,
          PostCommandDelay = 35,
        },
      };
      var service = new DelaySettingsFileService(applicationDirectory);

      service.Save(settings);
      DelaySettings savedSettings = new YamlService<DelaySettings>(settingsPath).Load();

      Assert.Equal(150, savedSettings.BreakdownTester.PostTestDelay);
      Assert.Equal(25, savedSettings.ModuleRelayControl.PreCommandDelay);
      Assert.Equal(35, savedSettings.ModuleRelayControl.PostCommandDelay);
    }
    finally
    {
      Directory.Delete(applicationDirectory, recursive: true);
    }
  }

  private static string CreateApplicationDirectory()
    => Path.Combine(Path.GetTempPath(), $"AskMkiM-delay-settings-{Guid.NewGuid():N}");

  private static string GetSettingsPath(string applicationDirectory)
    => Path.Combine(
      applicationDirectory,
      "Settings",
      DelaySettingsFileService.SettingsFileName);
}
