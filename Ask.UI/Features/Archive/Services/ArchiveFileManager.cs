using System.IO;
using System.IO.Compression;
using System.Text;
using static Ask.LogLib.LoggerUtility;

namespace Ask.UI.Features.Archive.Services
{
  /// <summary>
  /// Выполняет операции управления файлами внутри архивов.
  /// </summary>
  internal sealed class ArchiveFileManager
  {
    /// <summary>
    /// Расширение файлов архивов APKW.
    /// </summary>
    private const string ArchiveExtension = ".apkw";

    /// <summary>
    /// Расширение файлов записей внутри архива.
    /// </summary>
    private const string ArchiveEntryExtension = ".opkw";

    /// <summary>
    /// Переносит или копирует файл между архивами с поддержкой отката при ошибках.
    /// </summary>
    /// <param name="sourceArchivePath">Путь к исходному архиву.</param>
    /// <param name="archiveEntryName">Имя файла внутри архива.</param>
    /// <param name="targetArchivePath">Путь к целевому архиву.</param>
    /// <param name="removeSource">
    /// Признак удаления исходного файла после переноса.
    /// true — перемещение, false — копирование.
    /// </param>
    /// <returns>Имя перенесённой записи архива.</returns>
    public string TransferFile(string sourceArchivePath, string archiveEntryName, string targetArchivePath, bool removeSource)
    {
      var fullSourceArchivePath = ValidateArchivePath(sourceArchivePath);
      var fullTargetArchivePath = ValidateArchivePath(targetArchivePath);
      var normalizedArchiveEntryName = ResolveArchiveEntryName(archiveEntryName);

      ValidateArchiveEntryExtension(normalizedArchiveEntryName, nameof(archiveEntryName));

      if (string.Equals(fullSourceArchivePath, fullTargetArchivePath, StringComparison.OrdinalIgnoreCase))
      {
        var message = "Копирование и вставка файла в тот же архив не поддерживаются.";
        LogError(message);
        throw new InvalidOperationException(message);
      }

      var fileContent = ReadEntryBytes(fullSourceArchivePath, normalizedArchiveEntryName);
      AddEntry(fullTargetArchivePath, normalizedArchiveEntryName, fileContent);

      if (!removeSource)
      {
        return normalizedArchiveEntryName;
      }

      try
      {
        DeleteEntry(fullSourceArchivePath, normalizedArchiveEntryName);
        return normalizedArchiveEntryName;
      }
      catch (Exception deleteException)
      {
        try
        {
          DeleteEntry(fullTargetArchivePath, normalizedArchiveEntryName);
        }
        catch (Exception rollbackException)
        {
          var rollbackMessage =
            $"Не удалось завершить перенос файла '{normalizedArchiveEntryName}' и откатить целевой архив '{fullTargetArchivePath}'.";
          LogError($"{rollbackMessage} {rollbackException}");
          throw new IOException(rollbackMessage, rollbackException);
        }

        var message =
          $"Не удалось удалить исходный файл '{normalizedArchiveEntryName}' из архива '{fullSourceArchivePath}'. Изменения в целевом архиве отменены.";
        LogError($"{message} {deleteException}");
        throw new IOException(message, deleteException);
      }
    }

    /// <summary>
    /// Добавляет файл в архив с обновлением манифеста и проверкой ограничений.
    /// </summary>
    /// <param name="archivePath">Путь к архиву.</param>
    /// <param name="filePath">Путь к добавляемому файлу.</param>
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

    /// <summary>
    /// Добавляет сформированные строки данных в архивный файл и обновляет манифест.
    /// </summary>
    /// <param name="sourceLines">Исходные строки данных для записи.</param>
    /// <param name="archivePath">Путь к архиву.</param>
    /// <param name="fileName">Имя создаваемого файла внутри архива.</param>
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

    /// <summary>
    /// Удаляет файл из архива и обновляет манифест архива.
    /// </summary>
    /// <param name="archivePath">Путь к архиву.</param>
    /// <param name="archiveEntryName">Имя удаляемой записи внутри архива.</param>
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

    /// <summary>
    /// Удаляет архив с диска.
    /// </summary>
    /// <param name="archivePath">Путь к архиву.</param>
    public void DeleteArchive(string archivePath)
    {
      var fullArchivePath = ValidateArchivePath(archivePath);
      File.Delete(fullArchivePath);
    }

    /// <summary>
    /// Считывает содержимое файла из архива в виде массива байтов.
    /// </summary>
    /// <param name="archivePath">Путь к архиву.</param>
    /// <param name="normalizedArchiveEntryName">Нормализованное имя записи архива.</param>
    /// <returns>Массив байтов содержимого файла.</returns>
    private static byte[] ReadEntryBytes(string archivePath, string normalizedArchiveEntryName)
    {
      using (var encryptionSession = ArchiveEncryptionSession.Acquire(archivePath))
      using (var archiveStream = new FileStream(archivePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
      using (var archive = new ZipArchive(archiveStream, ZipArchiveMode.Read, leaveOpen: false))
      {
        var sourceEntry = FindArchiveEntry(archive, normalizedArchiveEntryName);
        if (sourceEntry == null)
        {
          var message = $"Файл '{normalizedArchiveEntryName}' не найден в архиве '{archivePath}'.";
          LogError(message);
          throw new FileNotFoundException(message, normalizedArchiveEntryName);
        }

        using (var sourceStream = sourceEntry.Open())
        using (var buffer = new MemoryStream())
        {
          sourceStream.CopyTo(buffer);
          return buffer.ToArray();
        }
      }
    }

    /// <summary>
    /// Добавляет новую запись в архив и обновляет манифест.
    /// </summary>
    /// <param name="archivePath">Путь к архиву.</param>
    /// <param name="normalizedArchiveEntryName">Нормализованное имя записи архива.</param>
    /// <param name="content">Содержимое файла в виде массива байтов.</param>
    private static void AddEntry(string archivePath, string normalizedArchiveEntryName, byte[] content)
    {
      using (var encryptionSession = ArchiveEncryptionSession.Acquire(archivePath))
      using (var archiveStream = new FileStream(archivePath, FileMode.Open, FileAccess.ReadWrite, FileShare.Read))
      using (var archive = new ZipArchive(archiveStream, ZipArchiveMode.Update, leaveOpen: false))
      {
        var fileAlreadyExists = FindArchiveEntry(archive, normalizedArchiveEntryName) != null;
        if (fileAlreadyExists)
        {
          var message = $"Файл '{normalizedArchiveEntryName}' уже существует в архиве '{archivePath}'.";
          LogError(message);
          throw new InvalidOperationException(message);
        }

        var archiveEntry = archive.CreateEntry(normalizedArchiveEntryName, CompressionLevel.Optimal);
        using (var targetStream = archiveEntry.Open())
        {
          targetStream.Write(content, 0, content.Length);
        }

        var manifestRecords = ArchiveManifestService.BuildManifestRecords(archive);
        ArchiveManifestService.WriteManifest(archive, manifestRecords);
      }
    }

    /// <summary>
    /// Удаляет запись из архива и обновляет манифест.
    /// </summary>
    /// <param name="archivePath">Путь к архиву.</param>
    /// <param name="normalizedArchiveEntryName">Нормализованное имя записи архива.</param>
    private static void DeleteEntry(string archivePath, string normalizedArchiveEntryName)
    {
      using (var encryptionSession = ArchiveEncryptionSession.Acquire(archivePath))
      using (var archiveStream = new FileStream(archivePath, FileMode.Open, FileAccess.ReadWrite, FileShare.Read))
      using (var archive = new ZipArchive(archiveStream, ZipArchiveMode.Update, leaveOpen: false))
      {
        var entryToDelete = FindArchiveEntry(archive, normalizedArchiveEntryName);
        if (entryToDelete == null)
        {
          var message = $"Файл '{normalizedArchiveEntryName}' не найден в архиве '{archivePath}'.";
          LogError(message);
          throw new FileNotFoundException(message, normalizedArchiveEntryName);
        }

        entryToDelete.Delete();
        ArchiveManifestService.RemoveFileRecord(archive, normalizedArchiveEntryName);
      }
    }

    /// <summary>
    /// Ищет запись файла в архиве по нормализованному имени.
    /// </summary>
    /// <param name="archive">ZIP-архив.</param>
    /// <param name="normalizedArchiveEntryName">Нормализованное имя записи.</param>
    /// <returns>Найденная запись архива или null.</returns>
    private static ZipArchiveEntry? FindArchiveEntry(ZipArchive archive, string normalizedArchiveEntryName)
    {
      return archive.Entries.FirstOrDefault(entry =>
        ArchiveManifestService.IsArchiveFileEntry(entry) &&
        ArchiveManifestService.NormalizeEntryName(entry.FullName)
          .Equals(normalizedArchiveEntryName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Формирует нормализованное имя записи архива на основе пути к файлу.
    /// </summary>
    /// <param name="filePath">Путь к файлу.</param>
    /// <returns>Нормализованное имя записи архива.</returns>
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

    /// <summary>
    /// Проверяет и нормализует имя записи архива.
    /// </summary>
    /// <param name="archiveEntryName">Имя записи архива.</param>
    /// <returns>Нормализованное имя записи архива.</returns>
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

    /// <summary>
    /// Проверяет допустимость расширения файла для добавления в архив.
    /// </summary>
    /// <param name="entryName">Имя файла или записи архива.</param>
    /// <param name="parameterName">Имя параметра для формирования исключения.</param>
    private static void ValidateArchiveEntryExtension(string entryName, string parameterName)
    {
      if (!string.Equals(Path.GetExtension(entryName), ArchiveEntryExtension, StringComparison.OrdinalIgnoreCase))
      {
        var message = "В архив можно добавлять только файлы с расширением .opkw.";
        LogError(message);
        throw new InvalidDataException(message);
      }
    }

    /// <summary>
    /// Проверяет корректность пути к архиву и возвращает полный путь.
    /// </summary>
    /// <param name="archivePath">Путь к архиву.</param>
    /// <returns>Полный нормализованный путь к архиву.</returns>
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
