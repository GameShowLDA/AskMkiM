namespace Ask.Core.Services.FileFormats.Opk
{
  /// <summary>
  /// Represents the outcome of converting a legacy OPK file into a translated OPKW file.
  /// </summary>
  public sealed class OpkToOpkwConversionResult
  {
    public string InputPath { get; init; } = string.Empty;

    public string? OutputPath { get; init; }

    public string? IntermediatePkPath { get; init; }

    public bool Success { get; init; }

    public string? ErrorMessage { get; init; }

    public int ErrorCount { get; init; }
  }
}
