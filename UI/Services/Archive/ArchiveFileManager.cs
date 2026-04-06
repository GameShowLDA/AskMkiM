using System.IO;
using System.IO.Compression;
using System.Text;
using static Ask.LogLib.LoggerUtility;

namespace UI.Services.Archive
{
  internal sealed class ArchiveFileManager
  {
    private const string ArchiveExtension = ".apkw";
    private const string ArchiveEntryExtension = ".opkw";

    public void AddFile(string archivePath, string filePath)
    {
      if (string.IsNullOrWhiteSpace(archivePath))
      {
        var message = $"Требуется указать путь к архиву.";
        LogError(message);
        throw new ArgumentException("Archive path is required.", nameof(archivePath));
      }

      if (string.IsNullOrWhiteSpace(filePath))
      {
        var message = $"Требуется указать путь к файлу.";
        LogError(message);
        throw new ArgumentException(message, nameof(filePath));
      }

      var fullArchivePath = Path.GetFullPath(archivePath);
      var fullFilePath = Path.GetFullPath(filePath);

      if (!File.Exists(fullArchivePath))
      {
        var message = $"Архив не был найден: {fullArchivePath}.";
        LogError(message);
        throw new FileNotFoundException(message, fullArchivePath);
      }

      if (!string.Equals(Path.GetExtension(fullArchivePath), ArchiveExtension, StringComparison.OrdinalIgnoreCase))
      {
        var message = $"Расширение архива не поддерживается. Ожидалось: {ArchiveExtension}";
        LogError(message);
        throw new InvalidDataException(message);
      }

      if (!File.Exists(fullFilePath))
      {
        var message = $"Файл не был найден: {fullFilePath}";
        LogError(message);
        throw new FileNotFoundException(message, fullFilePath);
      }

      ValidateArchiveEntryExtension(Path.GetFileName(fullFilePath), nameof(filePath));

      var normalizedArchiveEntryName = ResolveArchiveEntryNameFromFilePath(fullFilePath);
      if (normalizedArchiveEntryName.Equals(ArchiveManifestService.ManifestEntryName, StringComparison.OrdinalIgnoreCase))
      {
        var message = $"'{ArchiveManifestService.ManifestEntryName}' зарезервирован для архивных метаданных.";
        LogError(message);
        throw new InvalidOperationException(message);
      }

      using (var encryptionSession = ArchiveEncryptionSession.Acquire(fullArchivePath))
      using (var archiveStream = new FileStream(fullArchivePath, FileMode.Open, FileAccess.ReadWrite, FileShare.Read))
      using (var archive = new ZipArchive(archiveStream, ZipArchiveMode.Update, leaveOpen: false))
      {
        var fileAlreadyExists = archive.Entries.Any(entry =>
          ArchiveManifestService.IsArchiveFileEntry(entry) &&
          ArchiveManifestService.NormalizeEntryName(entry.FullName)
            .Equals(normalizedArchiveEntryName, StringComparison.OrdinalIgnoreCase));

        if (fileAlreadyExists)
        {
          var message = $"Файл '{normalizedArchiveEntryName}' уже существует в архиве '{fullArchivePath}'.";
          LogError(message);
          throw new InvalidOperationException(message);
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

    public void AddFile(List<List<string>> sourceLines, string archivePath, string fileName)
    {
      if (string.IsNullOrWhiteSpace(archivePath))
      {
        var message = "Требуется указать путь к архиву";
        LogError(message);
        throw new ArgumentException(message, nameof(archivePath));
      }

      var fullArchivePath = Path.GetFullPath(archivePath);
      var fullFilePathArchive = Path.Combine(fullArchivePath, fileName);

      if (!File.Exists(fullArchivePath))
      {
        var message = $"Архив не был найден: {fullArchivePath}";
        LogError(message);
        throw new FileNotFoundException(message, fullArchivePath);
      }

      if (!string.Equals(Path.GetExtension(fullArchivePath), ArchiveExtension, StringComparison.OrdinalIgnoreCase))
      {
        var message = $"Неподдерживаемое расширение архива. Ожидаемый:{ArchiveExtension}";
        LogError(message);
        throw new InvalidDataException(message);
      }

      var normalizedArchiveEntryName = ResolveArchiveEntryNameFromFilePath(fullFilePathArchive);
      ValidateArchiveEntryExtension(normalizedArchiveEntryName, nameof(fileName));
      if (normalizedArchiveEntryName.Equals(ArchiveManifestService.ManifestEntryName, StringComparison.OrdinalIgnoreCase))
      {
        var message = $"'{ArchiveManifestService.ManifestEntryName}' зарезервирован для архивных метаданных.";
        LogError(message);
        throw new InvalidOperationException(message);
      }

      using (var encryptionSession = ArchiveEncryptionSession.Acquire(fullArchivePath))
      using (var archiveStream = new FileStream(fullArchivePath, FileMode.Open, FileAccess.ReadWrite, FileShare.Read))
      using (var archive = new ZipArchive(archiveStream, ZipArchiveMode.Update, leaveOpen: false))
      {
        var fileAlreadyExists = archive.Entries.Any(entry =>
            ArchiveManifestService.IsArchiveFileEntry(entry) &&
            ArchiveManifestService.NormalizeEntryName(entry.FullName)
                .Equals(normalizedArchiveEntryName, StringComparison.OrdinalIgnoreCase));

        if (fileAlreadyExists)
        {
          var message = $"Файл '{normalizedArchiveEntryName}' уже существует в архиве '{fullArchivePath}'.";
          LogError(message);
          throw new InvalidOperationException(
              $"File '{normalizedArchiveEntryName}' already exists in archive '{fullArchivePath}'.");
        }

        var archiveEntry = archive.CreateEntry(normalizedArchiveEntryName, CompressionLevel.Optimal);

        using (var targetStream = archiveEntry.Open())
        using (var writer = new StreamWriter(targetStream, Encoding.UTF8, 65536)) // большой буфер
        {
          foreach (var row in sourceLines) // List<List<string>>
          {
            for (int i = 0; i < row.Count; i++)
            {
              if (i > 0)
                writer.Write("\n");

              writer.Write(row[i]);
            }

            writer.Write('\n');
          }
        }

        var manifestRecords = ArchiveManifestService.BuildManifestRecords(archive);
        ArchiveManifestService.WriteManifest(archive, manifestRecords);
      }
    }

    public void DeleteFile(string archivePath, string archiveEntryName)
    {
      var fullArchivePath = ValidateArchivePath(archivePath);
      var normalizedArchiveEntryName = ResolveArchiveEntryName(archiveEntryName);

      using (var encryptionSession = ArchiveEncryptionSession.Acquire(fullArchivePath))
      using (var archiveStream = new FileStream(fullArchivePath, FileMode.Open, FileAccess.ReadWrite, FileShare.Read))
      using (var archive = new ZipArchive(archiveStream, ZipArchiveMode.Update, leaveOpen: false))
      {
        var entryToDelete = archive.Entries.FirstOrDefault(entry =>
          ArchiveManifestService.IsArchiveFileEntry(entry) &&
          ArchiveManifestService.NormalizeEntryName(entry.FullName)
            .Equals(normalizedArchiveEntryName, StringComparison.OrdinalIgnoreCase));

        if (entryToDelete == null)
        {
          var message = $"Файл '{normalizedArchiveEntryName}' уже существует в архиве '{fullArchivePath}'.";
          LogError(message);
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
        var message = "Имя файла не может быть пустым";
        LogError(message);
        throw new ArgumentException(message, nameof(filePath));
      }

      if (normalizedName.EndsWith("/", StringComparison.Ordinal))
      {
        var message = "Имя файла должно указывать на файл, а не на каталог";
        LogError(message);
        throw new ArgumentException(message, nameof(filePath));
      }

      return normalizedName;
    }

    private static string ResolveArchiveEntryName(string archiveEntryName)
    {
      if (string.IsNullOrWhiteSpace(archiveEntryName))
      {
        var message = "Требуется указать имя записи в архиве";
        LogError(message);
        throw new ArgumentException(message, nameof(archiveEntryName));
      }

      var normalizedName = ArchiveManifestService.NormalizeEntryName(archiveEntryName.Trim());
      if (string.IsNullOrWhiteSpace(normalizedName))
      {
        var message = "Имя записи в архиве не может быть пустым";
        LogError(message);
        throw new ArgumentException(message, nameof(archiveEntryName));
      }

      if (normalizedName.EndsWith("/", StringComparison.Ordinal))
      {
        var message = "Имя записи в архиве должно указывать на файл, а не на каталог.";
        LogError(message);
        throw new ArgumentException(message, nameof(archiveEntryName));
      }

      return normalizedName;
    }

    private static void ValidateArchiveEntryExtension(string entryName, string parameterName)
    {
      if (!string.Equals(Path.GetExtension(entryName), ArchiveEntryExtension, StringComparison.OrdinalIgnoreCase))
      {
        var message = "В архив можно добавлять только файлы с расширением .opkw.";
        LogError(message);
        throw new InvalidDataException(message);
      }
    }

    private static string ValidateArchivePath(string archivePath)
    {
      if (string.IsNullOrWhiteSpace(archivePath))
      {
        var message = $"Требуется указать путь к архиву.";
        LogError(message);
        throw new ArgumentException(message, nameof(archivePath));
      }

      var fullArchivePath = Path.GetFullPath(archivePath);

      if (!File.Exists(fullArchivePath))
      {
        var message = $"Архив не был найден: {fullArchivePath}";
        LogError(message);
        throw new FileNotFoundException(message, fullArchivePath);
      }

      if (!string.Equals(Path.GetExtension(fullArchivePath), ArchiveExtension, StringComparison.OrdinalIgnoreCase))
      {
        var message = $"Расширение архива не поддерживается. Ожидалось: {ArchiveExtension}";
        LogError(message);
        throw new InvalidDataException(message);
      }

      return fullArchivePath;
    }
  }
}
