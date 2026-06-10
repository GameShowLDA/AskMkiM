using Ask.Diagnostics.Models;

namespace Ask.Diagnostics.Abstractions
{
  public interface ICommandHistoryService
  {
    void Add(string command);

    IReadOnlyList<CommandHistoryEntry> GetSnapshot();
  }
}
