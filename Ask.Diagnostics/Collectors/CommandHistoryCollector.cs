using Ask.Diagnostics.Abstractions;
using Ask.Diagnostics.Models;
using System.Text;

namespace Ask.Diagnostics.Collectors
{
  public sealed class CommandHistoryCollector : ICrashDataCollector
  {
    private readonly ICommandHistoryService _commandHistory;

    public CommandHistoryCollector(ICommandHistoryService commandHistory)
    {
      _commandHistory = commandHistory;
    }

    public string Name => "CommandHistory";

    public int Order => 300;

    public async Task CollectAsync(CrashContext context, CancellationToken cancellationToken = default)
    {
      var builder = new StringBuilder();
      var entries = _commandHistory.GetSnapshot();
      if (entries.Count == 0)
      {
        builder.AppendLine("No device commands were recorded before this diagnostic package was created.");
      }

      foreach (var entry in entries)
      {
        builder
          .Append(entry.Timestamp.ToLocalTime().ToString("HH:mm:ss.fff"))
          .Append(' ')
          .AppendLine(entry.Command);
      }

      await File.WriteAllTextAsync(
        Path.Combine(context.PackageDirectory, "commands.log"),
        builder.ToString(),
        Encoding.UTF8,
        cancellationToken).ConfigureAwait(false);
    }
  }
}
