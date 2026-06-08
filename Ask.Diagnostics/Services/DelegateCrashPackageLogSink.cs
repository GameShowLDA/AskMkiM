using Ask.Diagnostics.Abstractions;

namespace Ask.Diagnostics.Services
{
  internal sealed class DelegateCrashPackageLogSink : ICrashPackageLogSink
  {
    private readonly Action<string>? _information;
    private readonly Action<Exception, string>? _error;
    private readonly Action<string>? _packageCreated;

    public DelegateCrashPackageLogSink(Action<string>? information, Action<Exception, string>? error, Action<string>? packageCreated)
    {
      _information = information;
      _error = error;
      _packageCreated = packageCreated;
    }

    public void Information(string message)
    {
      try
      {
        _information?.Invoke(message);
      }
      catch
      {
      }
    }

    public void Error(Exception exception, string message)
    {
      try
      {
        ExceptionDiagnosticReporter.Suppress(exception);
        _error?.Invoke(exception, message);
      }
      catch
      {
      }
    }

    public void PackageCreated(string path)
    {
      try
      {
        _packageCreated?.Invoke(path);
      }
      catch
      {
      }
    }
  }
}
