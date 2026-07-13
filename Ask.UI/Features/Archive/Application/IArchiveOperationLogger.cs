namespace Ask.UI.Features.Archive.Application
{
  public interface IArchiveOperationLogger
  {
    void Started(ArchiveOperationKind operationKind, string operationName);

    void Succeeded(ArchiveOperationKind operationKind, string operationName);

    void Failed(ArchiveOperationKind operationKind, string operationName, Exception exception);
  }
}
