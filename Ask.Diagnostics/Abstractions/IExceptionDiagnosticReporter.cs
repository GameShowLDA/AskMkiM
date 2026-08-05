using Ask.Diagnostics.Models;

namespace Ask.Diagnostics.Abstractions
{
  public interface IExceptionDiagnosticReporter
  {
    void Report(Exception exception, string source);

    Task<string?> ReportAsync(
      Exception exception,
      string source,
      IReadOnlyList<CrashReportArtifact>? artifacts = null,
      CancellationToken cancellationToken = default);
  }
}
