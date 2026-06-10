using Ask.Diagnostics.Abstractions;
using Ask.Diagnostics.Models;

namespace Ask.Diagnostics.Services
{
  internal sealed class DelegateDiagnosticConfigProvider : IDiagnosticConfigProvider
  {
    private readonly Func<CrashContext, CancellationToken, Task<object?>> _capture;

    public DelegateDiagnosticConfigProvider(
      string name,
      Func<CrashContext, CancellationToken, Task<object?>> capture)
    {
      Name = string.IsNullOrWhiteSpace(name) ? "Config" : name;
      _capture = capture;
    }

    public string Name { get; }

    public Task<object?> CaptureAsync(CrashContext context, CancellationToken cancellationToken = default) =>
      _capture(context, cancellationToken);
  }
}
