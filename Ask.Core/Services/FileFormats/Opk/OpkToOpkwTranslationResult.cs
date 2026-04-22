namespace Ask.Core.Services.FileFormats.Opk
{
  /// <summary>
  /// Represents translation of an intermediate PK file into OPKW.
  /// </summary>
  public sealed class OpkToOpkwTranslationResult
  {
    public string? OutputPath { get; init; }

    public bool Success { get; init; }

    public string? ErrorMessage { get; init; }

    public int ErrorCount { get; init; }
  }
}
