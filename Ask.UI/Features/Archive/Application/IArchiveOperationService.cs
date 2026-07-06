namespace Ask.UI.Features.Archive.Application
{
  public interface IArchiveOperationService
  {
    bool CanEditArchives { get; }

    bool CanImportReadyArchives { get; }

    void EnsureCanEditArchives(ArchiveOperationKind operationKind);

    T ExecuteMutation<T>(
      ArchiveOperationKind operationKind,
      string operationName,
      Func<T> operation,
      Func<T, IEnumerable<string>>? archivePathsToValidate = null);

    void ExecuteMutation(
      ArchiveOperationKind operationKind,
      string operationName,
      Action operation,
      IEnumerable<string>? archivePathsToValidate = null);

    T ExecuteImport<T>(
      string operationName,
      Func<T> operation,
      Func<T, IEnumerable<string>>? archivePathsToValidate = null);

    void ValidateArchivesAfterChange(IEnumerable<string> archivePaths);
  }
}
