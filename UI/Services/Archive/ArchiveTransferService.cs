using Ask.Core.Services.EventCore.Events;
using Ask.Core.Services.EventCore.Services;
using System.IO;
using System.IO.Compression;
using Path = System.IO.Path;

namespace UI.Services.Archive
{
  public static class ArchiveTransferService
  {
    private const string ArchiveExtension = ".apkw";
    private const string DownloadedArchivesFolderName = "Скачанные архивы";

    /// <summary>
    /// Экспортирует указанный архив в выбранное пользователем место.
    /// </summary>
    /// <param name="archivePath">Путь к исходному архиву.</param>
    /// <param name="destinationFilePath">Путь, по которому архив будет сохранён.</param>
    /// <returns>
    /// Полный путь к экспортированному архиву.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Выбрасывается, если путь назначения не указан.
    /// </exception>
    /// <exception cref="DirectoryNotFoundException">
    /// Выбрасывается, если не удалось определить каталог назначения.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Выбрасывается, если исходный и целевой пути совпадают.
    /// </exception>
    /// <exception cref="FileNotFoundException">
    /// Выбрасывается, если исходный архив не найден.
    /// </exception>
    /// <exception cref="InvalidDataException">
    /// Выбрасывается, если файл не имеет поддерживаемого расширения.
    /// </exception>
    /// <remarks>
    /// Архив копируется в указанное место с перезаписью существующего файла.
    /// </remarks>
    public static string ExportArchive(string archivePath, string destinationFilePath)
    {
      var fullArchivePath = ValidateArchivePath(archivePath, nameof(archivePath));
      if (string.IsNullOrWhiteSpace(destinationFilePath))
      {
        throw new ArgumentException("Требуется указать путь сохранения архива.", nameof(destinationFilePath));
      }

      var fullDestinationFilePath = Path.GetFullPath(destinationFilePath);
      var destinationDirectory = Path.GetDirectoryName(fullDestinationFilePath);
      if (string.IsNullOrWhiteSpace(destinationDirectory))
      {
        throw new DirectoryNotFoundException("Не удалось определить каталог для сохранения архива.");
      }

      Directory.CreateDirectory(destinationDirectory);
      if (string.Equals(fullArchivePath, fullDestinationFilePath, StringComparison.OrdinalIgnoreCase))
      {
        throw new InvalidOperationException("Архив уже находится по выбранному пути.");
      }

      File.Copy(fullArchivePath, fullDestinationFilePath, overwrite: true);
      return fullDestinationFilePath;
    }

    /// <summary>
    /// Экспортирует все архивы из корневого каталога архивов в указанную папку.
    /// </summary>
    /// <param name="destinationRootDirectory">Папка, в которую будут экспортированы архивы.</param>
    /// <returns>
    /// Объект <see cref="ArchiveExportBatchResult"/>, содержащий путь к каталогу экспорта
    /// и количество экспортированных архивов.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Выбрасывается, если не указана папка назначения.
    /// </exception>
    /// <remarks>
    /// Все архивы копируются с сохранением структуры каталогов.
    /// Если в целевой папке уже существует каталог "Скачанные архивы",
    /// создаётся уникальный каталог с временной меткой.
    /// </remarks>
    public static ArchiveExportBatchResult ExportAllArchives(string destinationRootDirectory)
    {
      if (string.IsNullOrWhiteSpace(destinationRootDirectory))
      {
        throw new ArgumentException("Требуется указать папку для сохранения архивов.", nameof(destinationRootDirectory));
      }

      var archivesRootPath = ArchiveDirectoryService.ResolveArchivesRootPath();
      var fullDestinationRootDirectory = Path.GetFullPath(destinationRootDirectory);
      Directory.CreateDirectory(fullDestinationRootDirectory);

      var archivePaths = Directory.EnumerateFiles(archivesRootPath, "*" + ArchiveExtension, SearchOption.AllDirectories)
        .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
        .ToList();

      if (archivePaths.Count == 0)
      {
        return new ArchiveExportBatchResult(null, 0);
      }

      var exportDirectory = CreateUniqueDownloadDirectory(fullDestinationRootDirectory);
      foreach (var archivePath in archivePaths)
      {
        var relativePath = Path.GetRelativePath(archivesRootPath, archivePath);
        var targetFilePath = Path.Combine(exportDirectory, relativePath);
        var targetDirectory = Path.GetDirectoryName(targetFilePath);
        if (!string.IsNullOrWhiteSpace(targetDirectory))
        {
          Directory.CreateDirectory(targetDirectory);
        }

        File.Copy(archivePath, targetFilePath, overwrite: true);
      }

      return new ArchiveExportBatchResult(exportDirectory, archivePaths.Count);
    }

    /// <summary>
    /// Импортирует архив в корневой каталог архивов приложения.
    /// </summary>
    /// <param name="sourceArchivePath">Путь к импортируемому архиву.</param>
    /// <returns>
    /// Объект <see cref="ArchiveImportResult"/>, содержащий путь к импортированному архиву
    /// и флаг создания манифеста.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Выбрасывается, если путь к архиву не указан.
    /// </exception>
    /// <exception cref="FileNotFoundException">
    /// Выбрасывается, если архив не найден.
    /// </exception>
    /// <exception cref="InvalidDataException">
    /// Выбрасывается, если файл не является архивом поддерживаемого формата.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Выбрасывается, если архив уже существует в целевом каталоге
    /// или если выполняется попытка импорта самого себя.
    /// </exception>
    /// <remarks>
    /// После копирования проверяется наличие манифеста внутри архива.
    /// Если манифест отсутствует — он создаётся.
    /// Также публикуется событие об изменении списка архивов.
    /// В случае ошибки частично скопированный файл удаляется.
    /// </remarks>
    public static ArchiveImportResult ImportArchive(string sourceArchivePath)
    {
      var fullSourceArchivePath = ValidateArchivePath(sourceArchivePath, nameof(sourceArchivePath));
      var archivesRootPath = ArchiveDirectoryService.ResolveArchivesRootPath();
      var destinationArchivePath = Path.Combine(archivesRootPath, Path.GetFileName(fullSourceArchivePath));

      if (string.Equals(fullSourceArchivePath, destinationArchivePath, StringComparison.OrdinalIgnoreCase))
      {
        throw new InvalidOperationException("Архив уже находится в папке Archives.");
      }

      if (File.Exists(destinationArchivePath))
      {
        throw new InvalidOperationException($"Архив '{Path.GetFileName(destinationArchivePath)}' уже существует в папке Archives.");
      }

      var copied = false;
      try
      {
        File.Copy(fullSourceArchivePath, destinationArchivePath, overwrite: false);
        copied = true;

        var manifestCreated = EnsureManifestExists(destinationArchivePath);
        EventAggregator.Publish(new ArchiveEvents.Changed(ArchiveEvents.ArchiveChangeKind.ArchiveCreated, destinationArchivePath));

        return new ArchiveImportResult(destinationArchivePath, manifestCreated);
      }
      catch
      {
        if (copied && File.Exists(destinationArchivePath))
        {
          File.Delete(destinationArchivePath);
        }

        throw;
      }
    }

    private static bool EnsureManifestExists(string archivePath)
    {
      using var encryptionSession = ArchiveEncryptionSession.Acquire(archivePath);
      using var archiveStream = new FileStream(archivePath, FileMode.Open, FileAccess.ReadWrite, FileShare.Read);
      using var archive = new ZipArchive(archiveStream, ZipArchiveMode.Update, leaveOpen: false);

      if (archive.GetEntry(ArchiveManifestService.ManifestEntryName) != null)
      {
        return false;
      }

      var manifestRecords = ArchiveManifestService.BuildManifestRecords(archive);
      ArchiveManifestService.WriteManifest(archive, manifestRecords);
      return true;
    }

    private static string CreateUniqueDownloadDirectory(string destinationRootDirectory)
    {
      var targetDirectory = Path.Combine(destinationRootDirectory, DownloadedArchivesFolderName);
      if (!Directory.Exists(targetDirectory))
      {
        return Directory.CreateDirectory(targetDirectory).FullName;
      }

      var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
      var uniqueDirectory = Path.Combine(destinationRootDirectory, $"{DownloadedArchivesFolderName} {timestamp}");
      return Directory.CreateDirectory(uniqueDirectory).FullName;
    }

    private static string ValidateArchivePath(string archivePath, string parameterName)
    {
      if (string.IsNullOrWhiteSpace(archivePath))
      {
        throw new ArgumentException("Требуется указать путь к архиву.", parameterName);
      }

      var fullArchivePath = Path.GetFullPath(archivePath);
      if (!File.Exists(fullArchivePath))
      {
        throw new FileNotFoundException($"Архив не найден: {fullArchivePath}", fullArchivePath);
      }

      if (!string.Equals(Path.GetExtension(fullArchivePath), ArchiveExtension, StringComparison.OrdinalIgnoreCase))
      {
        throw new InvalidDataException($"Поддерживаются только архивы с расширением {ArchiveExtension}.");
      }

      return fullArchivePath;
    }
  }

  public sealed record ArchiveExportBatchResult(string? DestinationDirectory, int ExportedCount);

  public sealed record ArchiveImportResult(string ImportedArchivePath, bool ManifestCreated);
}
