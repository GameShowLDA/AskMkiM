using System.Text.Json;
using AskMkiM.Updater.Core;
using AskMkiM.Updater.Core.Models;

namespace AskMkiM.Updater.Core.Tests;

public sealed class UpdatePackageTests
{
  [Fact]
  public void VersionComparer_BlocksIdenticalBuild()
  {
    var current = new ApplicationVersionInfo("AskMkiM.exe", "1.2.0", "1.2.0+same");
    var update = new UpdateManifest { Version = "1.2.0", BuildIdentifier = "1.2.0+same" };

    Assert.NotEmpty(ApplicationVersionComparer.GetBlockingReason(current, update));
  }

  [Fact]
  public void VersionComparer_BlocksOlderVersion()
  {
    var current = new ApplicationVersionInfo("AskMkiM.exe", "2.0.0", "2.0.0+current");
    var update = new UpdateManifest { Version = "1.9.0", BuildIdentifier = "1.9.0+old" };

    Assert.NotEmpty(ApplicationVersionComparer.GetBlockingReason(current, update));
  }

  [Fact]
  public void VersionComparer_AllowsNewBuildWithSameProductVersion()
  {
    var current = new ApplicationVersionInfo("AskMkiM.exe", "1.2.0", "1.2.0+old");
    var update = new UpdateManifest { Version = "1.2.0", BuildIdentifier = "1.2.0+new" };

    Assert.Empty(ApplicationVersionComparer.GetBlockingReason(current, update));
  }

  [Fact]
  public async Task BuildAndOpen_EmbedsProgramFilesAndExcludesMutableData()
  {
    using var temp = new TemporaryDirectory();
    string source = temp.Combine("source");
    Directory.CreateDirectory(Path.Combine(source, "Resources"));
    Directory.CreateDirectory(Path.Combine(source, "Settings"));
    File.WriteAllText(Path.Combine(source, "AskMkiM.exe"), "new executable");
    File.WriteAllText(Path.Combine(source, "Ask.Engine.dll"), "engine");
    File.WriteAllText(Path.Combine(source, "Resources", "app.db"), "database");
    File.WriteAllText(Path.Combine(source, "Settings", "user.yaml"), "settings");
    WriteBuildManifest(source, "2.0.0", "2.0.0+test");

    string stub = temp.Combine("stub.exe");
    string update = temp.Combine("AskMkiM_Update_2.0.0.exe");
    File.WriteAllText(stub, "stub executable bytes");

    var builder = new UpdatePackageBuilder();
    var manifest = await builder.BuildAsync(source, stub, update);
    using OpenedUpdatePackage package = await UpdatePackageReader.OpenAsync(update);

    Assert.Equal("2.0.0", manifest.Version);
    Assert.Equal("2.0.0+test", package.Manifest.BuildIdentifier);
    Assert.Contains(package.Manifest.Files, file => file.Path == "AskMkiM.exe");
    Assert.Contains(package.Manifest.Files, file => file.Path == "Ask.Engine.dll");
    Assert.DoesNotContain(package.Manifest.Files, file => file.Path == "Resources/app.db");
    Assert.DoesNotContain(package.Manifest.Files, file => file.Path.StartsWith("Settings/"));
  }

  [Fact]
  public async Task Install_UpdatesProgramFilesAndPreservesSettingsAndDatabase()
  {
    using var temp = new TemporaryDirectory();
    string source = temp.Combine("source");
    string target = temp.Combine("target");
    Directory.CreateDirectory(Path.Combine(source, "Resources"));
    Directory.CreateDirectory(Path.Combine(source, "Settings"));
    Directory.CreateDirectory(Path.Combine(target, "Resources"));
    Directory.CreateDirectory(Path.Combine(target, "Settings"));

    File.WriteAllText(Path.Combine(source, "AskMkiM.exe"), "new executable");
    File.WriteAllText(Path.Combine(source, "Ask.Engine.dll"), "new engine");
    File.WriteAllText(Path.Combine(source, "Resources", "app.db"), "new database must be ignored");
    WriteBuildManifest(source, "2.0.0", "2.0.0+new");

    File.WriteAllText(Path.Combine(target, "AskMkiM.exe"), "old executable");
    File.WriteAllText(Path.Combine(target, "Ask.Engine.dll"), "old engine");
    File.WriteAllText(Path.Combine(target, "Resources", "app.db"), "production database");
    File.WriteAllText(Path.Combine(target, "Settings", "delaySettings.yaml"), "production settings");
    WriteBuildManifest(target, "1.0.0", "1.0.0+old");

    string stub = temp.Combine("stub.exe");
    string update = temp.Combine("update.exe");
    File.WriteAllText(stub, "stub");
    await new UpdatePackageBuilder().BuildAsync(source, stub, update);
    using OpenedUpdatePackage package = await UpdatePackageReader.OpenAsync(update);

    UpdateInstallationResult result = await new UpdateInstallationService().InstallAsync(
      package,
      Path.Combine(target, "AskMkiM.exe"));

    Assert.Equal("1.0.0", result.PreviousVersion.Version);
    Assert.Equal("new executable", File.ReadAllText(Path.Combine(target, "AskMkiM.exe")));
    Assert.Equal("new engine", File.ReadAllText(Path.Combine(target, "Ask.Engine.dll")));
    Assert.Equal("production database", File.ReadAllText(Path.Combine(target, "Resources", "app.db")));
    Assert.Equal("production settings", File.ReadAllText(Path.Combine(target, "Settings", "delaySettings.yaml")));
    Assert.True(File.Exists(Path.Combine(result.BackupDirectory, "DataSnapshot", "Resources", "app.db")));
    Assert.True(File.Exists(Path.Combine(result.BackupDirectory, "DataSnapshot", "Settings", "delaySettings.yaml")));
  }

  [Fact]
  public async Task Install_RemovesOnlyFilesManagedByPreviousUpdate()
  {
    using var temp = new TemporaryDirectory();
    string firstSource = temp.Combine("source-1");
    string secondSource = temp.Combine("source-2");
    string target = temp.Combine("target");
    Directory.CreateDirectory(firstSource);
    Directory.CreateDirectory(secondSource);
    Directory.CreateDirectory(target);

    PrepareProgram(firstSource, "1.0.0", "first");
    File.WriteAllText(Path.Combine(firstSource, "Obsolete.dll"), "managed obsolete file");
    PrepareProgram(secondSource, "2.0.0", "second");
    PrepareProgram(target, "0.9.0", "installed");
    File.WriteAllText(Path.Combine(target, "UnknownPlugin.dll"), "must stay");

    string stub = temp.Combine("stub.exe");
    File.WriteAllText(stub, "stub");
    await InstallBuiltPackageAsync(temp, firstSource, stub, target, "first-update.exe");
    Assert.True(File.Exists(Path.Combine(target, "Obsolete.dll")));

    await InstallBuiltPackageAsync(temp, secondSource, stub, target, "second-update.exe");

    Assert.False(File.Exists(Path.Combine(target, "Obsolete.dll")));
    Assert.True(File.Exists(Path.Combine(target, "UnknownPlugin.dll")));
  }

  [Fact]
  public async Task Install_RestoresAlreadyReplacedFilesWhenLaterReplacementFails()
  {
    using var temp = new TemporaryDirectory();
    string source = temp.Combine("source");
    string target = temp.Combine("target");
    Directory.CreateDirectory(source);
    Directory.CreateDirectory(target);
    PrepareProgram(source, "2.0.0", "new executable");
    PrepareProgram(target, "1.0.0", "old executable");
    File.WriteAllText(Path.Combine(source, "ZLocked.dll"), "new locked file");
    string lockedTarget = Path.Combine(target, "ZLocked.dll");
    File.WriteAllText(lockedTarget, "old locked file");
    File.SetAttributes(lockedTarget, FileAttributes.ReadOnly);

    string stub = temp.Combine("stub.exe");
    string update = temp.Combine("rollback-update.exe");
    File.WriteAllText(stub, "stub");
    await new UpdatePackageBuilder().BuildAsync(source, stub, update);
    using OpenedUpdatePackage package = await UpdatePackageReader.OpenAsync(update);

    try
    {
      await Assert.ThrowsAsync<UnauthorizedAccessException>(() => new UpdateInstallationService().InstallAsync(
        package,
        Path.Combine(target, "AskMkiM.exe")));
      Assert.Equal("old executable", File.ReadAllText(Path.Combine(target, "AskMkiM.exe")));
    }
    finally
    {
      File.SetAttributes(lockedTarget, FileAttributes.Normal);
    }
  }

  private static async Task InstallBuiltPackageAsync(
    TemporaryDirectory temp,
    string source,
    string stub,
    string target,
    string outputName)
  {
    string update = temp.Combine(outputName);
    await new UpdatePackageBuilder().BuildAsync(source, stub, update);
    using OpenedUpdatePackage package = await UpdatePackageReader.OpenAsync(update);
    await new UpdateInstallationService().InstallAsync(package, Path.Combine(target, "AskMkiM.exe"));
  }

  private static void PrepareProgram(string directory, string version, string executableContents)
  {
    Directory.CreateDirectory(directory);
    File.WriteAllText(Path.Combine(directory, "AskMkiM.exe"), executableContents);
    WriteBuildManifest(directory, version, $"{version}+test");
  }

  private static void WriteBuildManifest(string directory, string version, string buildIdentifier)
  {
    File.WriteAllText(
      Path.Combine(directory, "build-manifest.json"),
      JsonSerializer.Serialize(new { Version = version, BuildIdentifier = buildIdentifier }));
  }
}
