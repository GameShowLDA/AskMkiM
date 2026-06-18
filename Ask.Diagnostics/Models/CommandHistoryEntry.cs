namespace Ask.Diagnostics.Models
{
  public sealed record CommandHistoryEntry(DateTimeOffset Timestamp, string Command);
}
