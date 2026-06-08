using Ask.Diagnostics.Abstractions;
using Ask.Diagnostics.Infrastructure;
using Ask.Diagnostics.Models;

namespace Ask.Diagnostics.Collectors
{
  public sealed class ExceptionCollector : ICrashDataCollector
  {
    public string Name => "Exception";

    public int Order => 100;

    public async Task CollectAsync(CrashContext context, CancellationToken cancellationToken = default)
    {
      var exception = context.Exception;
      var payload = new
      {
        type = exception.GetType().Name,
        fullType = exception.GetType().FullName,
        message = exception.Message,
        source = exception.Data["CrashSource"]?.ToString(),
        innerException = exception.InnerException?.ToString(),
        stackTrace = exception.StackTrace,
        timestamp = context.Timestamp,
      };

      await JsonFileWriter.WriteAsync(
        Path.Combine(context.PackageDirectory, "crash.json"),
        payload,
        cancellationToken).ConfigureAwait(false);

      await File.WriteAllTextAsync(
        Path.Combine(context.PackageDirectory, "stacktrace.txt"),
        exception.ToString(),
        cancellationToken).ConfigureAwait(false);
    }
  }
}
