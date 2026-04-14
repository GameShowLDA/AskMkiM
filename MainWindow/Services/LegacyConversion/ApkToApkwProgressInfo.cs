namespace MainWindowProgram.Services.LegacyConversion
{
  /// <summary>
  /// Progress snapshot for APK to APKW conversion.
  /// </summary>
  public sealed class ApkToApkwProgressInfo
  {
    public string Stage { get; init; } = string.Empty;

    public string Hint { get; init; } = string.Empty;

    public string? CurrentFileName { get; init; }

    public int ProcessedEntries { get; init; }

    public int TotalEntries { get; init; }

    public double Percent { get; init; }
  }
}
