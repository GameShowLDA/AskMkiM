using Ask.Diagnostics.Abstractions;
using Ask.Diagnostics.Infrastructure;
using Ask.Diagnostics.Models;
using Ask.Diagnostics.Services;

namespace Ask.Diagnostics.Collectors
{
  public sealed class MetadataCollector : ICrashDataCollector
  {
    public string Name => "Metadata";

    public int Order => 1000;

    public Task CollectAsync(CrashContext context, CancellationToken cancellationToken = default) =>
      JsonFileWriter.WriteAsync(
        Path.Combine(context.PackageDirectory, "metadata.json"),
        CrashPackageService.BuildMetadata(context),
        cancellationToken);
  }
}
