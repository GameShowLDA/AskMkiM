using Ask.Diagnostics.Models;

namespace Ask.Diagnostics.Abstractions
{
  public interface IDiagnosticStateProvider
  {
    string Name { get; }

    Task<object?> CaptureAsync(CrashContext context, CancellationToken cancellationToken = default);
  }
}
