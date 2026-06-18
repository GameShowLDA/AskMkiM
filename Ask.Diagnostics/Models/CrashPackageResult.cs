namespace Ask.Diagnostics.Models
{
  public sealed class CrashPackageResult
  {
    public required string PackageDirectory { get; init; }

    public string? ZipPath { get; init; }

    public required string ReturnedPath { get; init; }

    public bool IsZipped => !string.IsNullOrWhiteSpace(ZipPath);
  }
}
