namespace Ask.UI.Features.Archive.Application
{
  public sealed class ArchiveOperationKind
  {
    public static readonly ArchiveOperationKind OpenArchive = new ArchiveOperationKind(nameof(OpenArchive));
    public static readonly ArchiveOperationKind CreateArchive = new ArchiveOperationKind(nameof(CreateArchive));
    public static readonly ArchiveOperationKind ImportReadyArchive = new ArchiveOperationKind(nameof(ImportReadyArchive));
    public static readonly ArchiveOperationKind ExportArchive = new ArchiveOperationKind(nameof(ExportArchive));
    public static readonly ArchiveOperationKind ExportAllArchives = new ArchiveOperationKind(nameof(ExportAllArchives));
    public static readonly ArchiveOperationKind AddFile = new ArchiveOperationKind(nameof(AddFile));
    public static readonly ArchiveOperationKind DeleteFile = new ArchiveOperationKind(nameof(DeleteFile));
    public static readonly ArchiveOperationKind DeleteArchive = new ArchiveOperationKind(nameof(DeleteArchive));
    public static readonly ArchiveOperationKind CopyFile = new ArchiveOperationKind(nameof(CopyFile));
    public static readonly ArchiveOperationKind MoveFile = new ArchiveOperationKind(nameof(MoveFile));
    public static readonly ArchiveOperationKind SaveGeneratedFile = new ArchiveOperationKind(nameof(SaveGeneratedFile));

    public ArchiveOperationKind(string key)
    {
      if (string.IsNullOrWhiteSpace(key))
      {
        throw new ArgumentException("Требуется указать ключ операции архива.", nameof(key));
      }

      Key = key;
    }

    public string Key { get; }

    public override string ToString() => Key;
  }
}
