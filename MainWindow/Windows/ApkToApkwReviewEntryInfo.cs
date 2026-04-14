namespace MainWindowProgram.Windows
{
  internal sealed class ApkToApkwReviewEntryInfo
  {
    public string FilePath { get; init; } = string.Empty;

    public string FileName { get; init; } = string.Empty;

    public string FileTypeDisplay { get; init; } = string.Empty;

    public int ErrorCount { get; init; }

    public string Name { get; init; } = string.Empty;

    public string? NameOK { get; init; }

    public string? OPK { get; init; }

    public string? Order { get; init; }

    public string OpkFileName { get; init; } = string.Empty;

    public string? Department { get; init; }

    public string? Comment { get; init; }

    public DateTime CreationDate { get; init; }
  }
}
