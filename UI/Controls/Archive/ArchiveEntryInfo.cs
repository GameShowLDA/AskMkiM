using System.IO;

namespace UI.Controls.Archive
{
  internal sealed class ArchiveEntryInfo
  {
    public string ArchivePath { get; }
    public string EntryName { get; }
    public string Name { get; }
    public string Extension => string.IsNullOrWhiteSpace(Path.GetExtension(Name)) ? "(none)" : Path.GetExtension(Name).ToLowerInvariant();
    public long SizeBytes { get; }
    public long PackedBytes { get; }
    public DateTime LastModified { get; }

    public ArchiveEntryInfo(string archivePath, string entryName, long sizeBytes, long packedBytes, DateTimeOffset lastModified)
    {
      ArchivePath = archivePath;
      EntryName = entryName;
      Name = entryName;
      SizeBytes = sizeBytes;
      PackedBytes = packedBytes;
      LastModified = lastModified.LocalDateTime;
    }
  }
}
