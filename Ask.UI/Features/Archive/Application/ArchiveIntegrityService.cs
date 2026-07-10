using Ask.UI.Features.Archive.Services;
using System.IO;
using System.IO.Compression;

namespace Ask.UI.Features.Archive.Application
{
  public sealed class ArchiveIntegrityService : IArchiveIntegrityService
  {
    public void ValidateArchive(string archivePath)
    {
      if (string.IsNullOrWhiteSpace(archivePath))
      {
        return;
      }

      var fullArchivePath = Path.GetFullPath(archivePath);
      if (!File.Exists(fullArchivePath))
      {
        return;
      }

      using var encryptionSession = ArchiveEncryptionSession.Acquire(fullArchivePath);
      using var stream = new FileStream(fullArchivePath, FileMode.Open, FileAccess.Read, FileShare.Read);
      using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
      var notifications = ArchiveManifestService.ValidateArchive(archive);

      if (notifications.Count == 0)
      {
        return;
      }

      throw new InvalidDataException(
        $"Нарушена целостность архива '{Path.GetFileName(fullArchivePath)}': {string.Join("; ", notifications)}");
    }

    public void ValidateArchives(IEnumerable<string> archivePaths)
    {
      if (archivePaths == null)
      {
        return;
      }

      foreach (var archivePath in archivePaths.Where(path => !string.IsNullOrWhiteSpace(path)).Distinct(StringComparer.OrdinalIgnoreCase))
      {
        ValidateArchive(archivePath);
      }
    }
  }
}
