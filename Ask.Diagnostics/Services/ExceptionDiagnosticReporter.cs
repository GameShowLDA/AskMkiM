using Ask.Diagnostics.Abstractions;
using Ask.Diagnostics.Configuration;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace Ask.Diagnostics.Services
{
  internal sealed class ExceptionDiagnosticReporter : IExceptionDiagnosticReporter
  {
    private readonly ICrashPackageService _crashPackageService;
    private readonly IOptions<CrashPackageOptions> _options;
    private readonly ICrashPackageLogSink _logSink;
    private readonly ConcurrentDictionary<string, DateTimeOffset> _lastReports = new(StringComparer.Ordinal);
    private int _pendingReports;

    public ExceptionDiagnosticReporter(
      ICrashPackageService crashPackageService,
      IOptions<CrashPackageOptions> options,
      ICrashPackageLogSink logSink)
    {
      _crashPackageService = crashPackageService;
      _options = options;
      _logSink = logSink;
    }

    public void Report(Exception exception, string source)
    {
      ArgumentNullException.ThrowIfNull(exception);

      var options = _options.Value;
      if (!options.CreatePackageForLoggedExceptions)
      {
        _logSink.Information($"Logged exception crash package skipped because reporting is disabled: {source}");
        return;
      }

      if (IsSuppressed(exception))
      {
        return;
      }

      var now = DateTimeOffset.UtcNow;
      var throttleWindow = options.LoggedExceptionThrottleWindow;
      var key = BuildThrottleKey(exception, source);
      if (IsThrottled(key, now, throttleWindow))
      {
        _logSink.Information($"Logged exception crash package skipped by throttle: {source}");
        return;
      }

      var maxPending = Math.Max(1, options.MaxPendingLoggedExceptionReports);
      if (Interlocked.Increment(ref _pendingReports) > maxPending)
      {
        Interlocked.Decrement(ref _pendingReports);
        _logSink.Information($"Logged exception crash package skipped because pending limit was reached: {source}");
        return;
      }

      _logSink.Information($"Logged exception crash package queued: {source}");
      _ = Task.Run(() => CreatePackageAsync(exception, source, options.LoggedExceptionReportTimeout));
    }

    private async Task CreatePackageAsync(Exception exception, string source, TimeSpan timeout)
    {
      try
      {
        if (!exception.Data.Contains(CrashPackageExceptionDataKeys.CrashSource))
        {
          exception.Data[CrashPackageExceptionDataKeys.CrashSource] = source;
        }

        using var cts = new CancellationTokenSource(timeout <= TimeSpan.Zero ? TimeSpan.FromSeconds(30) : timeout);
        await _crashPackageService.CreateAsync(exception, cts.Token).ConfigureAwait(false);
      }
      catch (Exception reportException)
      {
        Suppress(reportException);
        _logSink.Error(reportException, $"Logged exception crash package creation failed: {source}");
      }
      finally
      {
        Interlocked.Decrement(ref _pendingReports);
      }
    }

    private bool IsThrottled(string key, DateTimeOffset now, TimeSpan throttleWindow)
    {
      if (throttleWindow <= TimeSpan.Zero)
      {
        _lastReports[key] = now;
        return false;
      }

      if (_lastReports.TryGetValue(key, out var lastReport) && now - lastReport < throttleWindow)
      {
        return true;
      }

      _lastReports[key] = now;
      PruneOldThrottleKeys(now, throttleWindow + throttleWindow);
      return false;
    }

    private void PruneOldThrottleKeys(DateTimeOffset now, TimeSpan maxAge)
    {
      foreach (var pair in _lastReports)
      {
        if (now - pair.Value > maxAge)
        {
          _lastReports.TryRemove(pair.Key, out _);
        }
      }
    }

    private static string BuildThrottleKey(Exception exception, string source)
    {
      var firstStackLine = exception.StackTrace?
        .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
        .FirstOrDefault() ?? string.Empty;

      return string.Join("|", exception.GetType().FullName, exception.Message, source, firstStackLine);
    }

    private static bool IsSuppressed(Exception exception)
    {
      return exception.Data.Contains(CrashPackageExceptionDataKeys.SuppressAutoReport);
    }

    internal static void Suppress(Exception exception)
    {
      exception.Data[CrashPackageExceptionDataKeys.SuppressAutoReport] = true;
    }
  }
}
