using Ask.Diagnostics.Abstractions;
using Ask.Diagnostics.Configuration;
using Ask.Diagnostics.Extensions;
using Ask.Diagnostics.Models;
using Microsoft.Extensions.DependencyInjection;
using System.IO.Compression;

namespace Ask.Diagnostics.UnitTests;

public sealed class CrashPackageServiceTests
{
  [Fact]
  public async Task CreateAsync_IncludesExceptionAndCallerArtifactsInArchive()
  {
    var rootDirectory = Path.Combine(
      Path.GetTempPath(),
      "AskMkiM.CrashPackageTests",
      Guid.NewGuid().ToString("N"));

    try
    {
      var services = new ServiceCollection();
      services.AddCrashDiagnostics(options =>
      {
        options.Path = rootDirectory;
        options.AutoZip = true;
        options.IncludeScreenshot = false;
        options.IncludeLogs = false;
        options.IncludeConfig = false;
        options.CleanupPolicy = CrashPackageCleanupPolicy.None;
      });

      await using var provider = services.BuildServiceProvider();
      var service = provider.GetRequiredService<ICrashPackageService>();
      var exception = CaptureException();
      var artifacts = new[]
      {
        CrashReportArtifact.Json("translation-parameters.json", new { operation = "CreateNewTranslator" }),
        CrashReportArtifact.Text("source-program.txt", "10 СИ"),
      };

      var packagePath = await service.CreateAsync(exception, artifacts);

      Assert.EndsWith(".zip", packagePath, StringComparison.OrdinalIgnoreCase);
      Assert.True(File.Exists(packagePath));

      using var archive = ZipFile.OpenRead(packagePath);
      Assert.NotNull(archive.GetEntry("crash.json"));
      Assert.NotNull(archive.GetEntry("stacktrace.txt"));
      Assert.NotNull(archive.GetEntry("system-info.json"));
      Assert.NotNull(archive.GetEntry("translation-parameters.json"));

      var sourceEntry = Assert.IsType<ZipArchiveEntry>(archive.GetEntry("source-program.txt"));
      using var reader = new StreamReader(sourceEntry.Open());
      Assert.Equal("10 СИ", await reader.ReadToEndAsync());
    }
    finally
    {
      if (Directory.Exists(rootDirectory))
      {
        Directory.Delete(rootDirectory, recursive: true);
      }
    }
  }

  private static Exception CaptureException()
  {
    try
    {
      throw new InvalidOperationException("translation failed", new ArgumentException("invalid source"));
    }
    catch (Exception exception)
    {
      return exception;
    }
  }
}
