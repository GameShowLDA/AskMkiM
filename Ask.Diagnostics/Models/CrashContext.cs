using System.Collections.Concurrent;

namespace Ask.Diagnostics.Models
{
  public sealed class CrashContext
  {
    private readonly ConcurrentDictionary<string, string> _collectorFailures = new();

    public CrashContext(
      Exception exception,
      DateTimeOffset timestamp,
      string rootDirectory,
      string packageDirectory,
      string packageName)
    {
      Exception = exception ?? throw new ArgumentNullException(nameof(exception));
      Timestamp = timestamp;
      RootDirectory = rootDirectory ?? throw new ArgumentNullException(nameof(rootDirectory));
      PackageDirectory = packageDirectory ?? throw new ArgumentNullException(nameof(packageDirectory));
      PackageName = packageName ?? throw new ArgumentNullException(nameof(packageName));
    }

    public Exception Exception { get; }

    public DateTimeOffset Timestamp { get; }

    public string RootDirectory { get; }

    public string PackageDirectory { get; }

    public string PackageName { get; }

    public IReadOnlyDictionary<string, string> CollectorFailures => _collectorFailures;

    public string? ZipPath { get; set; }

    public void AddCollectorFailure(string collectorName, Exception exception)
    {
      _collectorFailures[collectorName] = exception.ToString();
    }
  }
}
