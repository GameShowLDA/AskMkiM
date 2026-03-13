using Ask.Core.Services.FilesUtility;
using Ask.Core.Shared.Metadata.Static;
using System.IO;
using System.IO.Compression;
using static Ask.LogLib.LoggerUtility;

namespace UI.Services.Archive
{
  public class ArchiveCreationService
  {
    private const string ArchiveExtension = ".apkw";

    public string Create(string archiveName)
    {
      var normalizedArchiveName = NormalizeArchiveName(archiveName);
      var archivesDirectoryPath = EnsureArchivesDirectory();
      var archivePath = Path.Combine(archivesDirectoryPath, normalizedArchiveName + ArchiveExtension);

      if (File.Exists(archivePath))
      {
        var message = $"Архив '{Path.GetFileName(archivePath)}' уже существует.";
        LogError(message);
        throw new InvalidOperationException(message);
      }

      using (var archiveStream = new FileStream(archivePath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None))
      using (var archive = new ZipArchive(archiveStream, ZipArchiveMode.Update, leaveOpen: false))
      {
        ArchiveManifestService.WriteManifest(archive, new List<ArchiveManifestFileRecord>());
      }

      FileEncryptionManager.EncryptFile(archivePath);

      return archivePath;
    }

    private string EnsureArchivesDirectory()
    {
      var baseDir = new DirectoryInfo(AppContext.BaseDirectory);
      var archivesDirectoryPath = Path.Combine(baseDir.FullName, FileLocations.ArchiveDirectory);
      var directoryInfo = Directory.CreateDirectory(archivesDirectoryPath);
     
      if ((directoryInfo.Attributes & FileAttributes.Hidden) == 0)
      {
          directoryInfo.Attributes |= FileAttributes.Hidden;
      }

      return directoryInfo.FullName;
    }

    private static string NormalizeArchiveName(string archiveName)
    {
      if (string.IsNullOrWhiteSpace(archiveName))
      {
        var message = "Требуется указать имя архива";
        LogError(message);
        throw new ArgumentException(message, nameof(archiveName));
      }

      var normalizedName = Path.GetFileNameWithoutExtension(archiveName.Trim());

      foreach (var invalidChar in Path.GetInvalidFileNameChars())
      {
        normalizedName = normalizedName.Replace(invalidChar, '_');
      }

      if (string.IsNullOrWhiteSpace(normalizedName))
      {
        var message = "Имя архива содержит только недопустимые символы.";
        LogError(message);
        throw new ArgumentException(message, nameof(archiveName));
      }

      return normalizedName;
    }
  }
}
