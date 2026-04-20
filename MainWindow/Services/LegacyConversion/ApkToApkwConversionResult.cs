namespace MainWindowProgram.Services.LegacyConversion
{
  using System.Collections.Generic;

  /// <summary>
  /// Represents the outcome of converting a legacy APK archive into a modern APKW archive.
  /// </summary>
  public sealed class ApkToApkwConversionResult
  {
    public string InputPath { get; init; } = string.Empty;

    public string? CreatedArchivePath { get; init; }

    public string? IntermediateDirectoryPath { get; init; }

    public string? FailedEntryName { get; init; }

    public string? FailedSourceOpkPath { get; init; }

    public IReadOnlyList<string> ProblemPkPaths { get; init; } = [];

    public int FailedEntriesCount { get; init; }

    public int TranslationErrorCount { get; init; }

    public int TranslationFailedFilesCount { get; init; }

    public int PreparationFailedFilesCount { get; init; }

    public bool Success { get; init; }

    public string? ErrorMessage { get; init; }

    public int EntriesCount { get; init; }
  }
}
