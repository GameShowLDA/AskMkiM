using Ask.Diagnostics.Abstractions;
using Ask.Diagnostics.Infrastructure;
using Ask.Diagnostics.Models;

namespace Ask.Diagnostics.Collectors
{
  public sealed class DeviceStateCollector : ICrashDataCollector
  {
    private readonly IEnumerable<IDiagnosticStateProvider> _providers;

    public DeviceStateCollector(IEnumerable<IDiagnosticStateProvider> providers)
    {
      _providers = providers;
    }

    public string Name => "DeviceState";

    public int Order => 400;

    public async Task CollectAsync(CrashContext context, CancellationToken cancellationToken = default)
    {
      var state = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
      var errors = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

      foreach (var provider in _providers)
      {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
          state[provider.Name] = await provider.CaptureAsync(context, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
          errors[provider.Name] = ex.ToString();
        }
      }

      await JsonFileWriter.WriteAsync(
        Path.Combine(context.PackageDirectory, "device-state.json"),
        new { state, providerErrors = errors },
        cancellationToken).ConfigureAwait(false);
    }
  }
}
