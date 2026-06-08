using Ask.Diagnostics.Abstractions;
using Ask.Diagnostics.Configuration;
using Ask.Diagnostics.Infrastructure;
using Ask.Diagnostics.Models;
using Microsoft.Extensions.Options;

namespace Ask.Diagnostics.Collectors
{
  public sealed class ConfigCollector : ICrashDataCollector
  {
    private readonly IEnumerable<IDiagnosticConfigProvider> _providers;
    private readonly IOptions<CrashPackageOptions> _options;

    public ConfigCollector(IEnumerable<IDiagnosticConfigProvider> providers, IOptions<CrashPackageOptions> options)
    {
      _providers = providers;
      _options = options;
    }

    public string Name => "Config";

    public int Order => 500;

    public async Task CollectAsync(CrashContext context, CancellationToken cancellationToken = default)
    {
      if (!_options.Value.IncludeConfig)
      {
        return;
      }

      var providers = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
      var providerErrors = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

      foreach (var provider in _providers)
      {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
          providers[provider.Name] = await provider.CaptureAsync(context, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
          providerErrors[provider.Name] = ex.ToString();
        }
      }

      var files = await CaptureConfigFilesAsync(_options.Value.ConfigFilePaths, cancellationToken).ConfigureAwait(false);

      await JsonFileWriter.WriteAsync(
        Path.Combine(context.PackageDirectory, "config.json"),
        new { providers, providerErrors, files },
        cancellationToken).ConfigureAwait(false);
    }

    private static async Task<IReadOnlyList<object>> CaptureConfigFilesAsync(
      IEnumerable<string> configuredPaths,
      CancellationToken cancellationToken)
    {
      var files = new List<object>();

      foreach (var configuredPath in ExpandConfiguredPaths(configuredPaths))
      {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
          var content = await File.ReadAllTextAsync(configuredPath, cancellationToken).ConfigureAwait(false);
          files.Add(new
          {
            path = configuredPath,
            fileName = Path.GetFileName(configuredPath),
            content,
          });
        }
        catch (Exception ex)
        {
          files.Add(new
          {
            path = configuredPath,
            error = ex.Message,
          });
        }
      }

      return files;
    }

    private static IEnumerable<string> ExpandConfiguredPaths(IEnumerable<string> configuredPaths)
    {
      foreach (var configuredPath in configuredPaths.Where(static path => !string.IsNullOrWhiteSpace(path)))
      {
        var expanded = Environment.ExpandEnvironmentVariables(configuredPath);

        if (File.Exists(expanded))
        {
          yield return Path.GetFullPath(expanded);
          continue;
        }

        if (!Directory.Exists(expanded))
        {
          continue;
        }

        foreach (var file in Directory.EnumerateFiles(expanded, "*.*", SearchOption.TopDirectoryOnly)
          .Where(static file =>
            file.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
            || file.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase)
            || file.EndsWith(".yml", StringComparison.OrdinalIgnoreCase)
            || file.EndsWith(".config", StringComparison.OrdinalIgnoreCase))
          .OrderBy(static file => file, StringComparer.OrdinalIgnoreCase)
          .Take(50))
        {
          yield return Path.GetFullPath(file);
        }
      }
    }
  }
}
