using System.IO.Compression;
using System.Text;
using System.Text.Json;
using AskMkiM.Updater.Core.Models;

namespace AskMkiM.Updater.Core;

/// <summary>
/// Извлекает пакет, присоединённый к автономному установщику.
/// </summary>
public static class UpdatePackageReader
{
  /// <summary>
  /// Открывает пакет из указанного исполняемого файла.
  /// </summary>
  public static async Task<OpenedUpdatePackage> OpenAsync(
    string executablePath,
    CancellationToken cancellationToken = default)
  {
    string tempArchive = Path.Combine(Path.GetTempPath(), $"AskMkiM.Update.{Guid.NewGuid():N}.zip");
    try
    {
      await using FileStream source = File.Open(executablePath, FileMode.Open, FileAccess.Read, FileShare.Read);
      if (source.Length < PackageConstants.TrailerSize)
      {
        throw new InvalidDataException("Файл не содержит пакет обновления.");
      }

      source.Seek(-PackageConstants.TrailerSize, SeekOrigin.End);
      var lengthBytes = new byte[sizeof(long)];
      await source.ReadExactlyAsync(lengthBytes, cancellationToken);
      var magicBytes = new byte[PackageConstants.TrailerMagic.Length];
      await source.ReadExactlyAsync(magicBytes, cancellationToken);
      if (!Encoding.ASCII.GetString(magicBytes).Equals(PackageConstants.TrailerMagic, StringComparison.Ordinal))
      {
        throw new InvalidDataException("Файл не является обновлением AskMkiM.");
      }

      long archiveLength = BitConverter.ToInt64(lengthBytes);
      long archiveOffset = source.Length - PackageConstants.TrailerSize - archiveLength;
      if (archiveLength <= 0 || archiveOffset < 0)
      {
        throw new InvalidDataException("Повреждён размер встроенного пакета.");
      }

      source.Position = archiveOffset;
      await using (FileStream destination = new(tempArchive, FileMode.CreateNew, FileAccess.Write, FileShare.None))
      {
        await CopyExactlyAsync(source, destination, archiveLength, cancellationToken);
      }

      UpdateManifest manifest = await ReadAndValidateManifestAsync(tempArchive, cancellationToken);
      return new OpenedUpdatePackage(tempArchive, manifest);
    }
    catch
    {
      if (File.Exists(tempArchive)) File.Delete(tempArchive);
      throw;
    }
  }

  private static async Task<UpdateManifest> ReadAndValidateManifestAsync(
    string archivePath,
    CancellationToken cancellationToken)
  {
    using ZipArchive archive = ZipFile.OpenRead(archivePath);
    ZipArchiveEntry entry = archive.GetEntry(PackageConstants.ManifestEntryName)
      ?? throw new InvalidDataException("В пакете отсутствует манифест.");
    await using Stream stream = entry.Open();
    UpdateManifest manifest = await JsonSerializer.DeserializeAsync<UpdateManifest>(stream, cancellationToken: cancellationToken)
      ?? throw new InvalidDataException("Не удалось прочитать манифест обновления.");

    if (manifest.FormatVersion != 1
        || !string.Equals(manifest.Product, "AskMkiM", StringComparison.Ordinal)
        || string.IsNullOrWhiteSpace(manifest.Version)
        || manifest.Files.Count == 0)
    {
      throw new InvalidDataException("Манифест обновления имеет неподдерживаемый формат.");
    }

    var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    foreach (UpdateFileEntry file in manifest.Files)
    {
      string normalized = PathSafety.NormalizeRelativePath(file.Path);
      if (ProtectedPathPolicy.IsProtected(normalized) || !paths.Add(normalized))
      {
        throw new InvalidDataException($"Недопустимый путь в манифесте: {file.Path}");
      }

      ZipArchiveEntry payloadEntry = archive.GetEntry(normalized)
        ?? throw new InvalidDataException($"В архиве отсутствует файл {normalized}.");
      if (payloadEntry.Length != file.Length)
      {
        throw new InvalidDataException($"Размер файла {normalized} не соответствует манифесту.");
      }
    }

    return manifest;
  }

  private static async Task CopyExactlyAsync(
    Stream source,
    Stream destination,
    long length,
    CancellationToken cancellationToken)
  {
    var buffer = new byte[81920];
    long remaining = length;
    while (remaining > 0)
    {
      int read = await source.ReadAsync(buffer.AsMemory(0, (int)Math.Min(buffer.Length, remaining)), cancellationToken);
      if (read == 0) throw new EndOfStreamException("Встроенный пакет неожиданно завершился.");
      await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
      remaining -= read;
    }
  }
}
