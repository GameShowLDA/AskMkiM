using Ask.Diagnostics.Abstractions;
using Ask.Diagnostics.Configuration;
using Ask.Diagnostics.Models;
using Microsoft.Extensions.Options;

namespace Ask.Diagnostics.Services
{
  public sealed class CommandHistoryService : ICommandHistoryService
  {
    private readonly object _gate = new();
    private readonly int _capacity;
    private readonly Queue<CommandHistoryEntry> _entries;

    public CommandHistoryService(IOptions<CrashPackageOptions> options)
    {
      _capacity = Math.Max(1, options.Value.CommandHistoryCapacity);
      _entries = new Queue<CommandHistoryEntry>(_capacity);
    }

    public void Add(string command)
    {
      if (string.IsNullOrWhiteSpace(command))
      {
        return;
      }

      lock (_gate)
      {
        while (_entries.Count >= _capacity)
        {
          _entries.Dequeue();
        }

        _entries.Enqueue(new CommandHistoryEntry(DateTimeOffset.Now, command.Trim()));
      }
    }

    public IReadOnlyList<CommandHistoryEntry> GetSnapshot()
    {
      lock (_gate)
      {
        return _entries.ToArray();
      }
    }
  }
}
