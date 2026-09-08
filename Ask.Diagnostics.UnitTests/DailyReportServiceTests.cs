using Ask.Diagnostics.Services;
using System.IO.Compression;

namespace Ask.Diagnostics.UnitTests;

public sealed class DailyReportServiceTests
{
  [Fact]
  public async Task Report_SelectsDayAndPreservesNestedFiles()
  {
    string root = Path.Combine(Path.GetTempPath(), "ask-daily-test-" + Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(root);
    try
    {
      foreach (var relative in new[] { "history/2026-09-08", "history/2026-09-07", "logs/2026-09-08", "crash/20260908_120000_Error/nested", "crash/20260907_120000_Error" })
        Directory.CreateDirectory(Path.Combine(root, relative));
      await File.WriteAllTextAsync(Path.Combine(root, "history/2026-09-08/test.asktrace"), "encrypted-protocol");
      await File.WriteAllTextAsync(Path.Combine(root, "history/2026-09-07/old.asktrace"), "old");
      await File.WriteAllTextAsync(Path.Combine(root, "logs/2026-09-08/all.log"), "complete-log");
      await File.WriteAllTextAsync(Path.Combine(root, "crash/20260908_120000_Error/nested/stack.txt"), "stack");
      string destination = Path.Combine(root, "report.zip");
      var issues = await new DailyReportService().CreateAsync(new DateTime(2026, 9, 8), destination,
        Path.Combine(root, "history"), Path.Combine(root, "logs"), Path.Combine(root, "crash"));
      Assert.Empty(issues);
      using var archive = ZipFile.OpenRead(destination);
      Assert.Equal(4, archive.Entries.Count);
      Assert.NotNull(archive.GetEntry("Protocols/test.asktrace"));
      Assert.NotNull(archive.GetEntry("CrashReports/20260908_120000_Error/nested/stack.txt"));
      Assert.Null(archive.GetEntry("Protocols/old.asktrace"));
      using var reader = new StreamReader(archive.GetEntry("Logs/all.log")!.Open());
      Assert.Equal("complete-log", await reader.ReadToEndAsync());
    }
    finally { Directory.Delete(root, recursive: true); }
  }

  [Fact]
  public async Task MissingSources_AreReportedInsideReadableArchive()
  {
    string root = Path.Combine(Path.GetTempPath(), "ask-daily-test-" + Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(root);
    try
    {
      string destination = Path.Combine(root, "report.zip");
      var issues = await new DailyReportService().CreateAsync(DateTime.Today, destination,
        Path.Combine(root, "history"), Path.Combine(root, "logs"), Path.Combine(root, "crash"));
      Assert.Equal(3, issues.Count);
      using var archive = ZipFile.OpenRead(destination);
      Assert.Single(archive.Entries);
      Assert.NotNull(archive.GetEntry("report.json"));
    }
    finally { Directory.Delete(root, recursive: true); }
  }
}
