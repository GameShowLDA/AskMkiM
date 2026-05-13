using System.IO;
using System.IO.Compression;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using static Ask.LogLib.LoggerUtility;

namespace Ask.UI.Features.Archive.Services
{
  /// <summary>
  /// Предоставляет методы для работы с манифестом архивов.
  /// </summary>
  internal static class ArchiveManifestService
  {
    /// <summary>
    /// Имя файла манифеста внутри архива.
    /// </summary>
    public const string ManifestEntryName = "__apkw_manifest.json";

    /// <summary>
    /// Проверяет целостность архива по данным манифеста и контрольным суммам.
    /// </summary>
    /// <param name="archive">ZIP-архив.</param>
    /// <returns>Список уведомлений о нарушениях целостности архива.</returns>
    public static IReadOnlyList<string> ValidateArchive(ZipArchive archive)
    {
      ArgumentNullException.ThrowIfNull(archive);

      var notifications = new List<string>();
      var manifest = ReadManifest(archive);
      var archiveFiles = archive.Entries
        .Where(IsArchiveFileEntry)
        .ToDictionary(entry => NormalizeEntryName(entry.FullName), entry => entry, StringComparer.OrdinalIgnoreCase);

      var manifestFiles = manifest.Files
        .Where(file => !string.IsNullOrWhiteSpace(file.Name))
        .GroupBy(file => NormalizeEntryName(file.Name), StringComparer.OrdinalIgnoreCase)
        .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

      foreach (var archiveFile in archiveFiles)
      {
        if (!manifestFiles.TryGetValue(archiveFile.Key, out var manifestFile))
        {
          notifications.Add($"Checksum record is missing for file '{archiveFile.Key}'.");
          continue;
        }

        var actualChecksum = ComputeChecksum(archiveFile.Value);
        if (!actualChecksum.Equals(NormalizeChecksum(manifestFile.Checksum), StringComparison.OrdinalIgnoreCase))
        {
          notifications.Add(
            $"Checksum mismatch for file '{archiveFile.Key}'. " +
            $"Expected '{manifestFile.Checksum}', actual '{actualChecksum}'.");
        }
      }

      foreach (var manifestFile in manifestFiles.Values)
      {
        var normalizedName = NormalizeEntryName(manifestFile.Name);
        if (!archiveFiles.ContainsKey(normalizedName))
        {
          notifications.Add($"File '{normalizedName}' is listed in manifest but is missing from archive.");
        }
      }

      return notifications;
    }

    /// <summary>
    /// Формирует список записей манифеста на основе содержимого архива.
    /// </summary>
    /// <param name="archive">ZIP-архив.</param>
    /// <returns>Список записей манифеста.</returns>
    public static List<ArchiveManifestFileRecord> BuildManifestRecords(ZipArchive archive)
    {
      ArgumentNullException.ThrowIfNull(archive);

      return archive.Entries
        .Where(IsArchiveFileEntry)
        .Select(entry => new ArchiveManifestFileRecord
        {
          Name = NormalizeEntryName(entry.FullName),
          Checksum = ComputeChecksum(entry), 
        })
        .OrderBy(record => record.Name, StringComparer.OrdinalIgnoreCase)
        .ToList();
    }

    /// <summary>
    /// Записывает манифест архива на основе списка файлов.
    /// </summary>
    /// <param name="archive">ZIP-архив.</param>
    /// <param name="fileRecords">Коллекция записей файлов для манифеста.</param>
    public static void WriteManifest(ZipArchive archive, IEnumerable<ArchiveManifestFileRecord> fileRecords)
    {
      ArgumentNullException.ThrowIfNull(archive);

      var records = (fileRecords ?? Enumerable.Empty<ArchiveManifestFileRecord>())
        .Where(record => record != null && !string.IsNullOrWhiteSpace(record.Name))
        .Select(record => new ArchiveManifestFileRecord
        {
          Name = NormalizeEntryName(record.Name),
          Checksum = NormalizeChecksum(record.Checksum),
          CreationDate = DateOnly.FromDateTime(DateTime.UtcNow).ToString("o")
        })
        .OrderBy(record => record.Name, StringComparer.OrdinalIgnoreCase)
        .ToList();

      var manifest = new ArchiveManifest
      {
        Files = records
      };

      var existingManifestEntry = archive.GetEntry(ManifestEntryName);
      existingManifestEntry?.Delete();

      var manifestEntry = archive.CreateEntry(ManifestEntryName, CompressionLevel.Optimal);
      var serializer = new DataContractJsonSerializer(typeof(ArchiveManifest));

      using (var manifestStream = manifestEntry.Open())
      {
        serializer.WriteObject(manifestStream, manifest);
      }
    }

    /// <summary>
    /// Удаляет запись файла из манифеста архива.
    /// </summary>
    /// <param name="archive">ZIP-архив.</param>
    /// <param name="archiveEntryName">Имя записи архива.</param>
    public static void RemoveFileRecord(ZipArchive archive, string archiveEntryName)
    {
      ArgumentNullException.ThrowIfNull(archive);

      if (string.IsNullOrWhiteSpace(archiveEntryName))
      {
        var message = $"Требуется указать имя записи в архиве.";
        LogError(message);
        throw new ArgumentException(message, nameof(archiveEntryName));
      }

      var normalizedEntryName = NormalizeEntryName(archiveEntryName);
      var manifest = ReadManifest(archive);

      manifest.Files = manifest.Files
        .Where(record =>
          !NormalizeEntryName(record.Name).Equals(normalizedEntryName, StringComparison.OrdinalIgnoreCase))
        .ToList();

      WriteManifest(archive, manifest.Files);
    }

    /// <summary>
    /// Проверяет, является ли запись пользовательским файлом архива.
    /// </summary>
    /// <param name="entry">Запись ZIP-архива.</param>
    /// <returns>
    /// true, если запись является файлом архива и не является манифестом;
    /// иначе false.
    /// </returns>
    public static bool IsArchiveFileEntry(ZipArchiveEntry entry)
    {
      return entry != null &&
             !string.IsNullOrEmpty(entry.Name) &&
             !entry.FullName.Equals(ManifestEntryName, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Нормализует имя записи архива.
    /// </summary>
    /// <param name="entryName">Исходное имя записи.</param>
    /// <returns>Нормализованное имя записи.</returns>
    public static string NormalizeEntryName(string entryName)
    {
      if (entryName == null)
      {
        return string.Empty;
      }

      var normalizedName = entryName.Replace('\\', '/');
      while (normalizedName.StartsWith("/", StringComparison.Ordinal))
      {
        normalizedName = normalizedName.Substring(1);
      }

      return normalizedName.Trim();
    }

    /// <summary>
    /// Считывает и десериализует манифест архива.
    /// </summary>
    /// <param name="archive">ZIP-архив.</param>
    /// <returns>Объект манифеста архива.</returns>
    private static ArchiveManifest ReadManifest(ZipArchive archive)
    {
      var manifestEntry = archive.GetEntry(ManifestEntryName);
      if (manifestEntry == null)
      {
        return new ArchiveManifest
        {
          Files = new List<ArchiveManifestFileRecord>()
        };
      }

      var serializer = new DataContractJsonSerializer(typeof(ArchiveManifest));
      using (var stream = manifestEntry.Open())
      {
        var manifest = serializer.ReadObject(stream) as ArchiveManifest;
        if (manifest?.Files == null)
        {
          return new ArchiveManifest
          {
            Files = new List<ArchiveManifestFileRecord>()
          };
        }

        manifest.Files = manifest.Files
          .Where(file => file != null && !string.IsNullOrWhiteSpace(file.Name))
          .Select(file => new ArchiveManifestFileRecord
          {
            Name = NormalizeEntryName(file.Name),
            Checksum = NormalizeChecksum(file.Checksum),
            CreationDate = file.CreationDate
          })
          .ToList();

        return manifest;
      }
    }

    /// <summary>
    /// Вычисляет контрольную сумму содержимого записи архива.
    /// </summary>
    /// <param name="entry">Запись ZIP-архива.</param>
    /// <returns>Контрольная сумма файла.</returns>
    private static string ComputeChecksum(ZipArchiveEntry entry)
    {
      using (var stream = entry.Open())
      {
        return ComputeChecksum(stream);
      }
    }

    /// <summary>
    /// Вычисляет SHA-256 контрольную сумму потока данных.
    /// </summary>
    /// <param name="stream">Поток данных.</param>
    /// <returns>Строковое представление контрольной суммы.</returns>
    private static string ComputeChecksum(Stream stream)
    {
      using (var sha256 = SHA256.Create())
      {
        var hash = sha256.ComputeHash(stream);
        return BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
      }
    }

    /// <summary>
    /// Нормализует строковое представление контрольной суммы.
    /// </summary>
    /// <param name="checksum">Исходная контрольная сумма.</param>
    /// <returns>Нормализованная контрольная сумма.</returns>
    private static string NormalizeChecksum(string checksum)
    {
      return (checksum ?? string.Empty).Trim().ToLowerInvariant();
    }
  }

  /// <summary>
  /// Представляет манифест архива с перечнем файлов.
  /// </summary>
  [DataContract]
  internal sealed class ArchiveManifest
  {
    /// <summary>
    /// Список файлов, содержащихся в архиве.
    /// </summary>
    [DataMember(Name = "files")]
    public List<ArchiveManifestFileRecord> Files { get; set; } = new List<ArchiveManifestFileRecord>();
  }

  /// <summary>
  /// Представляет запись файла в манифесте архива.
  /// </summary>
  [DataContract]
  internal sealed class ArchiveManifestFileRecord
  {
    /// <summary>
    /// Имя файла внутри архива.
    /// </summary>
    [DataMember(Name = "name")]
    public string Name { get; set; }

    /// <summary>
    /// Контрольная сумма файла.
    /// </summary>
    [DataMember(Name = "checksum")]
    public string Checksum { get; set; }

    /// <summary>
    /// Дата создания записи файла.
    /// </summary>
    [DataMember(Name = "creationdate")]
    public string CreationDate { get; set; }
  }
}
