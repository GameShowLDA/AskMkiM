using Ask.Diagnostics.Abstractions;
using Ask.Diagnostics.Configuration;
using Ask.Diagnostics.Infrastructure;
using Ask.Diagnostics.Models;
using Microsoft.Extensions.Options;
using System.IO.Compression;
using System.Text.RegularExpressions;

namespace Ask.Diagnostics.Services
{
  public sealed partial class CrashPackageService : ICrashPackageService
  {
    private readonly IReadOnlyList<ICrashDataCollector> _collectors;
    private readonly IOptions<CrashPackageOptions> _options;
    private readonly ICrashPackageLogSink _logSink;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public CrashPackageService(
      IEnumerable<ICrashDataCollector> collectors,
      IOptions<CrashPackageOptions> options,
      ICrashPackageLogSink logSink)
    {
      _collectors = collectors.OrderBy(static collector => collector.Order).ToArray();
      _options = options;
      _logSink = logSink;
    }

    public async Task<string> CreateAsync(Exception exception, CancellationToken cancellationToken = default)
    {
      ArgumentNullException.ThrowIfNull(exception);

      await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
      try
      {
        return await CreateCoreAsync(exception, cancellationToken).ConfigureAwait(false);
      }
      finally
      {
        _gate.Release();
      }
    }

    private async Task<string> CreateCoreAsync(Exception exception, CancellationToken cancellationToken)
    {
      var options = _options.Value;
      var timestamp = DateTimeOffset.Now;
      var rootDirectory = System.IO.Path.GetFullPath(ExpandPath(options.Path));
      Directory.CreateDirectory(rootDirectory);

      var exceptionName = SanitizeFileName(exception.GetType().Name);
      var packageName = $"{timestamp:yyyyMMdd_HHmmss}_{exceptionName}";
      var packageDirectory = CreateUniqueDirectory(rootDirectory, packageName);
      var context = new CrashContext(exception, timestamp, rootDirectory, packageDirectory, System.IO.Path.GetFileName(packageDirectory));

      foreach (var collector in _collectors)
      {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
          await collector.CollectAsync(context, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception collectorException)
        {
          context.AddCollectorFailure(collector.Name, collectorException);
          _logSink.Error(collectorException, $"Crash collector failed: {collector.Name}");
        }
      }

      if (options.AutoZip)
      {
        var zipPath = CreateUniqueFilePath(rootDirectory, $"{context.PackageName}.zip");
        context.ZipPath = zipPath;

        try
        {
          await JsonFileWriter.WriteAsync(
            System.IO.Path.Combine(packageDirectory, "metadata.json"),
            BuildMetadata(context),
            cancellationToken).ConfigureAwait(false);
          ZipFile.CreateFromDirectory(packageDirectory, zipPath, CompressionLevel.Optimal, includeBaseDirectory: false);
        }
        catch (Exception zipException)
        {
          context.ZipPath = null;
          context.AddCollectorFailure("Zip", zipException);
          await JsonFileWriter.WriteAsync(
            System.IO.Path.Combine(packageDirectory, "metadata.json"),
            BuildMetadata(context),
            cancellationToken).ConfigureAwait(false);
          _logSink.Error(zipException, "Crash package zip creation failed.");
        }
      }

      await CleanupAsync(rootDirectory, packageDirectory, context.ZipPath, options, cancellationToken).ConfigureAwait(false);

      var resultPath = context.ZipPath ?? packageDirectory;
      _logSink.Information($"Crash package created:{Environment.NewLine}{resultPath}");
      _logSink.PackageCreated(resultPath);
      return resultPath;
    }

    internal static object BuildMetadata(CrashContext context)
    {
      return new
      {
        packageName = context.PackageName,
        createdAt = context.Timestamp,
        directory = context.PackageDirectory,
        zip = context.ZipPath,
        collectorFailures = context.CollectorFailures,
      };
    }

    private static async Task CleanupAsync(
      string rootDirectory,
      string currentDirectory,
      string? currentZipPath,
      CrashPackageOptions options,
      CancellationToken cancellationToken)
    {
      if (options.CleanupPolicy == CrashPackageCleanupPolicy.None || options.MaxRetainedReports <= 0)
      {
        return;
      }

      await Task.Run(() =>
      {
        CleanupFiles(rootDirectory, "*.zip", currentZipPath, options.MaxRetainedReports);
        CleanupDirectories(rootDirectory, currentDirectory, options.MaxRetainedReports);
      }, cancellationToken).ConfigureAwait(false);
    }

    private static void CleanupFiles(string rootDirectory, string searchPattern, string? currentFile, int maxRetained)
    {
      foreach (var file in new DirectoryInfo(rootDirectory)
        .EnumerateFiles(searchPattern, SearchOption.TopDirectoryOnly)
        .Where(file => !string.Equals(file.FullName, currentFile, StringComparison.OrdinalIgnoreCase))
        .OrderByDescending(static file => file.LastWriteTimeUtc)
        .Skip(maxRetained - 1))
      {
        try
        {
          file.Delete();
        }
        catch
        {
        }
      }
    }

    private static void CleanupDirectories(string rootDirectory, string currentDirectory, int maxRetained)
    {
      foreach (var directory in new DirectoryInfo(rootDirectory)
        .EnumerateDirectories("*", SearchOption.TopDirectoryOnly)
        .Where(directory => !string.Equals(directory.FullName, currentDirectory, StringComparison.OrdinalIgnoreCase))
        .OrderByDescending(static directory => directory.LastWriteTimeUtc)
        .Skip(maxRetained - 1))
      {
        try
        {
          directory.Delete(recursive: true);
        }
        catch
        {
        }
      }
    }

    private static string CreateUniqueDirectory(string rootDirectory, string packageName)
    {
      for (var index = 0; ; index++)
      {
        var candidateName = index == 0 ? packageName : $"{packageName}_{index + 1}";
        var candidate = System.IO.Path.Combine(rootDirectory, candidateName);
        if (Directory.Exists(candidate))
        {
          continue;
        }

        try
        {
          Directory.CreateDirectory(candidate);
          return candidate;
        }
        catch (IOException)
        {
        }
      }
    }

    private static string CreateUniqueFilePath(string directory, string fileName)
    {
      var name = System.IO.Path.GetFileNameWithoutExtension(fileName);
      var extension = System.IO.Path.GetExtension(fileName);

      for (var index = 0; ; index++)
      {
        var candidateName = index == 0 ? fileName : $"{name}_{index + 1}{extension}";
        var candidate = System.IO.Path.Combine(directory, candidateName);
        if (!File.Exists(candidate))
        {
          return candidate;
        }
      }
    }

    private static string ExpandPath(string path)
    {
      if (string.IsNullOrWhiteSpace(path))
      {
        return System.IO.Path.Combine(AppContext.BaseDirectory, "CrashReports");
      }

      return Environment.ExpandEnvironmentVariables(path);
    }

    private static string SanitizeFileName(string value)
    {
      var invalidChars = Regex.Escape(new string(System.IO.Path.GetInvalidFileNameChars()));
      var sanitized = Regex.Replace(value, $"[{invalidChars}]+", "_", RegexOptions.CultureInvariant);
      return string.IsNullOrWhiteSpace(sanitized) ? "Exception" : sanitized;
    }
  }
}
