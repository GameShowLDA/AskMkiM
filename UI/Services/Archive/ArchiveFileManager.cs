using System.IO;
using System.IO.Compression;

namespace UI.Services.Archive
{
  internal sealed class ArchiveFileManager
  {
    private const string ArchiveExtension = ".apkw";

    public void AddFile(string archivePath, string filePath)
    {
      if (string.IsNullOrWhiteSpace(archivePath))
      {
        throw new ArgumentException("Archive path is required.", nameof(archivePath));
      }

      if (string.IsNullOrWhiteSpace(filePath))
      {
        throw new ArgumentException("File path is required.", nameof(filePath));
      }

      var fullArchivePath = Path.GetFullPath(archivePath);
      var fullFilePath = Path.GetFullPath(filePath);

      if (!File.Exists(fullArchivePath))
      {
        throw new FileNotFoundException($"Archive was not found: {fullArchivePath}", fullArchivePath);
      }

      if (!string.Equals(Path.GetExtension(fullArchivePath), ArchiveExtension, StringComparison.OrdinalIgnoreCase))
      {
        throw new InvalidDataException($"Unsupported archive extension. Expected: {ArchiveExtension}");
      }

      if (!File.Exists(fullFilePath))
      {
        throw new FileNotFoundException($"File was not found: {fullFilePath}", fullFilePath);
      }

      var normalizedArchiveEntryName = ResolveArchiveEntryNameFromFilePath(fullFilePath);
      if (normalizedArchiveEntryName.Equals(ArchiveManifestService.ManifestEntryName, StringComparison.OrdinalIgnoreCase))
      {
        throw new InvalidOperationException(
          $"'{ArchiveManifestService.ManifestEntryName}' is reserved for archive metadata.");
      }

      using (var archiveStream = new FileStream(fullArchivePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
      using (var archive = new ZipArchive(archiveStream, ZipArchiveMode.Update, leaveOpen: false))
      {
        var fileAlreadyExists = archive.Entries.Any(entry =>
          ArchiveManifestService.IsArchiveFileEntry(entry) &&
          ArchiveManifestService.NormalizeEntryName(entry.FullName)
            .Equals(normalizedArchiveEntryName, StringComparison.OrdinalIgnoreCase));

        if (fileAlreadyExists)
        {
          throw new InvalidOperationException(
            $"File '{normalizedArchiveEntryName}' already exists in archive '{fullArchivePath}'.");
        }

        var archiveEntry = archive.CreateEntry(normalizedArchiveEntryName, CompressionLevel.Optimal);
        using (var sourceFileStream = new FileStream(fullFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
        using (var targetStream = archiveEntry.Open())
        {
          sourceFileStream.CopyTo(targetStream);
        }

        var manifestRecords = ArchiveManifestService.BuildManifestRecords(archive);
        ArchiveManifestService.WriteManifest(archive, manifestRecords);
      }
    }

    public void DeleteFile(string archivePath, string archiveEntryName)
    {
      var fullArchivePath = ValidateArchivePath(archivePath);
      var normalizedArchiveEntryName = ResolveArchiveEntryName(archiveEntryName);

      using (var archiveStream = new FileStream(fullArchivePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
      using (var archive = new ZipArchive(archiveStream, ZipArchiveMode.Update, leaveOpen: false))
      {
        var entryToDelete = archive.Entries.FirstOrDefault(entry =>
          ArchiveManifestService.IsArchiveFileEntry(entry) &&
          ArchiveManifestService.NormalizeEntryName(entry.FullName)
            .Equals(normalizedArchiveEntryName, StringComparison.OrdinalIgnoreCase));

        if (entryToDelete == null)
        {
          throw new FileNotFoundException(
            $"File '{normalizedArchiveEntryName}' was not found in archive '{fullArchivePath}'.",
            normalizedArchiveEntryName);
        }

        entryToDelete.Delete();
        ArchiveManifestService.RemoveFileRecord(archive, normalizedArchiveEntryName);
      }
    }

    public void DeleteArchive(string archivePath)
    {
      var fullArchivePath = ValidateArchivePath(archivePath);
      File.Delete(fullArchivePath);
    }

    private static string ResolveArchiveEntryNameFromFilePath(string filePath)
    {
      var fileName = Path.GetFileName(filePath);
      var normalizedName = ArchiveManifestService.NormalizeEntryName(fileName);
      if (string.IsNullOrWhiteSpace(normalizedName))
      {
        throw new ArgumentException("File name cannot be empty.", nameof(filePath));
      }

      if (normalizedName.EndsWith("/", StringComparison.Ordinal))
      {
        throw new ArgumentException("File name must point to a file, not a directory.", nameof(filePath));
      }

      return normalizedName;
    }

    private static string ResolveArchiveEntryName(string archiveEntryName)
    {
      if (string.IsNullOrWhiteSpace(archiveEntryName))
      {
        throw new ArgumentException("Archive entry name is required.", nameof(archiveEntryName));
      }

      var normalizedName = ArchiveManifestService.NormalizeEntryName(archiveEntryName.Trim());
      if (string.IsNullOrWhiteSpace(normalizedName))
      {
        throw new ArgumentException("Archive entry name cannot be empty.", nameof(archiveEntryName));
      }

      if (normalizedName.EndsWith("/", StringComparison.Ordinal))
      {
        throw new ArgumentException("Archive entry name must point to a file, not a directory.", nameof(archiveEntryName));
      }

      return normalizedName;
    }

    private static string ValidateArchivePath(string archivePath)
    {
      if (string.IsNullOrWhiteSpace(archivePath))
      {
        throw new ArgumentException("Archive path is required.", nameof(archivePath));
      }

      var fullArchivePath = Path.GetFullPath(archivePath);

      if (!File.Exists(fullArchivePath))
      {
        throw new FileNotFoundException($"Archive was not found: {fullArchivePath}", fullArchivePath);
      }

      if (!string.Equals(Path.GetExtension(fullArchivePath), ArchiveExtension, StringComparison.OrdinalIgnoreCase))
      {
        throw new InvalidDataException($"Unsupported archive extension. Expected: {ArchiveExtension}");
      }

      return fullArchivePath;
    }
  }
}
