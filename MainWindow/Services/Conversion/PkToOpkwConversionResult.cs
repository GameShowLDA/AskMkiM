namespace MainWindowProgram.Services.Conversion
{
  /// <summary>
  /// Represents the outcome of converting a PK or PKW file into an OPKW file.
  /// </summary>
  public sealed class PkToOpkwConversionResult
  {
    public string InputPath { get; init; } = string.Empty;

    public string? OutputPath { get; init; }

    public bool SavedWithErrors { get; init; }

    public bool Success { get; init; }

    public string? ErrorMessage { get; init; }

    public int ErrorCount { get; init; }

    public int LinesCount { get; init; }

    public int WarningCount { get; init; }
  }
}
