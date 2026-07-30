using Ask.Diagnostics.Models;

namespace Ask.Diagnostics.Abstractions
{
  public interface ICrashPackageService
  {
    Task<string> CreateAsync(
      Exception exception,
      IReadOnlyList<CrashReportArtifact>? artifacts = null,
      CancellationToken cancellationToken = default);
  }
}
