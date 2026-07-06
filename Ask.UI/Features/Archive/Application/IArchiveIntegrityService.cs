namespace Ask.UI.Features.Archive.Application
{
  public interface IArchiveIntegrityService
  {
    void ValidateArchive(string archivePath);

    void ValidateArchives(IEnumerable<string> archivePaths);
  }
}
