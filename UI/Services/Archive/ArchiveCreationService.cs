using System.IO;
using System.IO.Compression;

namespace UI.Services.Archive
{
  public class ArchiveCreationService
  {
    private const string ArchiveExtension = ".apkw";
    private const string MainOutputPath = @"D:\AskMkiM\Bin";
    private const string ArchivesFolderName = "Archives";

    public string Create(string archiveName)
    {
      var normalizedArchiveName = NormalizeArchiveName(archiveName);
      var archivesDirectoryPath = EnsureArchivesDirectory();
      var archivePath = Path.Combine(archivesDirectoryPath, normalizedArchiveName + ArchiveExtension);

      if (File.Exists(archivePath))
      {
        File.Delete(archivePath);
      }

      using (var archiveStream = new FileStream(archivePath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None))
      using (var archive = new ZipArchive(archiveStream, ZipArchiveMode.Update, leaveOpen: false))
      {
        ArchiveManifestService.WriteManifest(archive, new List<ArchiveManifestFileRecord>());
      }

      return archivePath;
    }

    private static string EnsureArchivesDirectory()
    {
      var archivesDirectoryPath = Path.Combine(MainOutputPath, ArchivesFolderName);
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
        throw new ArgumentException("Archive name is required.", nameof(archiveName));
      }

      var normalizedName = Path.GetFileNameWithoutExtension(archiveName.Trim());

      foreach (var invalidChar in Path.GetInvalidFileNameChars())
      {
        normalizedName = normalizedName.Replace(invalidChar, '_');
      }

      if (string.IsNullOrWhiteSpace(normalizedName))
      {
        throw new ArgumentException("Archive name contains only invalid characters.", nameof(archiveName));
      }

      return normalizedName;
    }
  }
}
