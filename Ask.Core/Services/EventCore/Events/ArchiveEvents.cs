using Ask.Core.Shared.Interfaces.EventInterfaces;
using System.IO;

namespace Ask.Core.Services.EventCore.Events
{
  /// <summary>
  /// События, связанные с изменением списка архивов и файлов внутри архивов.
  /// </summary>
  public static class ArchiveEvents
  {
    public enum ArchiveChangeKind
    {
      ArchiveCreated,
      ArchiveEntriesChanged,
      ArchiveDeleted,
    }

    public sealed class Changed : IEvent
    {
      public ArchiveChangeKind ChangeKind { get; }
      public string ArchivePath { get; }

      public Changed(ArchiveChangeKind changeKind, string archivePath)
      {
        ChangeKind = changeKind;
        ArchivePath = string.IsNullOrWhiteSpace(archivePath)
          ? string.Empty
          : Path.GetFullPath(archivePath);
      }
    }
  }
}
