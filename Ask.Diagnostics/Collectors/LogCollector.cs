using Ask.Diagnostics.Abstractions;
using Ask.Diagnostics.Configuration;
using Ask.Diagnostics.Models;
using Microsoft.Extensions.Options;
using System.Text;

namespace Ask.Diagnostics.Collectors
{
  public sealed class LogCollector : ICrashDataCollector
  {
    private readonly IOptions<CrashPackageOptions> _options;

    public LogCollector(IOptions<CrashPackageOptions> options)
    {
      _options = options;
    }

    public string Name => "Logs";

    public int Order => 600;

    public async Task CollectAsync(CrashContext context, CancellationToken cancellationToken = default)
    {
      var options = _options.Value;
      if (!options.IncludeLogs)
      {
        return;
      }

      FlushNLog();

      var logFiles = ResolveLogFiles(options.LogFilePaths)
        .OrderByDescending(static file => File.GetLastWriteTimeUtc(file))
        .Take(10)
        .ToArray();

      var outputPath = Path.Combine(context.PackageDirectory, "app.log");
      await using var output = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.Read, 16 * 1024, useAsync: true);

      if (logFiles.Length == 0)
      {
        var message = Encoding.UTF8.GetBytes("No application log files were configured or found.");
        await output.WriteAsync(message, cancellationToken).ConfigureAwait(false);
        return;
      }

      foreach (var logFile in logFiles)
      {
        cancellationToken.ThrowIfCancellationRequested();

        var header = Encoding.UTF8.GetBytes(
          $"{Environment.NewLine}===== {logFile} ====={Environment.NewLine}");
        await output.WriteAsync(header, cancellationToken).ConfigureAwait(false);

        try
        {
          var tail = await ReadTailAsync(logFile, Math.Max(1024, options.MaxLogBytes), cancellationToken).ConfigureAwait(false);
          await output.WriteAsync(tail, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
          var error = Encoding.UTF8.GetBytes($"Failed to read log file: {ex}{Environment.NewLine}");
          await output.WriteAsync(error, cancellationToken).ConfigureAwait(false);
        }
      }
    }

    private static IEnumerable<string> ResolveLogFiles(IEnumerable<string> configuredPaths)
    {
      var paths = configuredPaths.Where(static path => !string.IsNullOrWhiteSpace(path)).ToList();

      if (paths.Count == 0)
      {
        paths.Add(Path.Combine(AppContext.BaseDirectory, "logs"));
        paths.Add(Path.Combine(AppContext.BaseDirectory, "Logs"));
        paths.Add(AppContext.BaseDirectory);
      }

      foreach (var configuredPath in paths)
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

        foreach (var file in Directory.EnumerateFiles(expanded, "*.log", SearchOption.AllDirectories))
        {
          yield return Path.GetFullPath(file);
        }
      }
    }

    private static void FlushNLog()
    {
      try
      {
        var logManagerType = Type.GetType("NLog.LogManager, NLog", throwOnError: false);
        var flushMethod = logManagerType?.GetMethod("Flush", new[] { typeof(TimeSpan) });
        flushMethod?.Invoke(null, new object[] { TimeSpan.FromSeconds(2) });
      }
      catch
      {
      }
    }

    private static async Task<byte[]> ReadTailAsync(string path, long maxBytes, CancellationToken cancellationToken)
    {
      await using var stream = new FileStream(
        path,
        FileMode.Open,
        FileAccess.Read,
        FileShare.ReadWrite | FileShare.Delete,
        bufferSize: 16 * 1024,
        useAsync: true);

      var bytesToRead = (int)Math.Min(maxBytes, stream.Length);
      stream.Seek(-bytesToRead, SeekOrigin.End);

      var buffer = new byte[bytesToRead];
      var totalRead = 0;
      while (totalRead < bytesToRead)
      {
        var read = await stream.ReadAsync(buffer.AsMemory(totalRead, bytesToRead - totalRead), cancellationToken)
          .ConfigureAwait(false);
        if (read == 0)
        {
          break;
        }

        totalRead += read;
      }

      return totalRead == buffer.Length ? buffer : buffer[..totalRead];
    }
  }
}
