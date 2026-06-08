using Ask.Diagnostics.Models;

namespace Ask.Diagnostics.Abstractions
{
  public interface ICrashDataCollector
  {
    string Name { get; }
    int Order { get; }
    Task CollectAsync(CrashContext context, CancellationToken cancellationToken = default);
  }
}
