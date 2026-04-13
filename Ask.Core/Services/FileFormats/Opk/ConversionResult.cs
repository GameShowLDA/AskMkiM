namespace Ask.Core.Services.FileFormats.Opk
{
  public sealed class ConversionResult
  {
    public string InputPath { get; init; } = null!;

    public string? OutputPath { get; init; }

    public string? MetadataPath { get; init; }

    public bool Success { get; init; }

    public string? ErrorMessage { get; init; }

    public int LinesCount { get; init; }
  }
}
