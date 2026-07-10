namespace Ask.UI.Features.Archive.Application
{
  public interface IArchivePermissionService
  {
    bool CanEditArchives { get; }

    bool CanImportReadyArchives { get; }

    void EnsureCanEditArchives(ArchiveOperationKind operationKind);

    void EnsureCanImportReadyArchives();
  }
}
