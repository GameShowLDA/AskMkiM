namespace Ask.Diagnostics.Configuration
{
  public sealed class CrashPackageOptions
  {
    public string Path { get; set; } = System.IO.Path.Combine(AppContext.BaseDirectory, "CrashReports");

    public int MaxRetainedReports { get; set; } = 20;

    public bool IncludeScreenshot { get; set; } = true;

    public bool IncludeLogs { get; set; } = true;

    public bool IncludeConfig { get; set; } = true;

    public bool AutoZip { get; set; } = true;

    public bool CreatePackageForLoggedExceptions { get; set; } = true;

    public TimeSpan LoggedExceptionThrottleWindow { get; set; } = TimeSpan.FromMinutes(2);

    public TimeSpan LoggedExceptionReportTimeout { get; set; } = TimeSpan.FromSeconds(30);

    public int MaxPendingLoggedExceptionReports { get; set; } = 2;

    public CrashPackageCleanupPolicy CleanupPolicy { get; set; } = CrashPackageCleanupPolicy.DeleteOldest;

    public int CommandHistoryCapacity { get; set; } = 500;

    public long MaxLogBytes { get; set; } = 5 * 1024 * 1024;

    public List<string> LogFilePaths { get; } = new();

    public List<string> ConfigFilePaths { get; } = new();
  }
}
