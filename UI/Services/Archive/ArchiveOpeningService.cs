using System.IO;
using System.IO.Compression;
using System.Text;

namespace UI.Services.Archive
{
  internal sealed class ArchiveOpeningService : IDisposable
  {
    private const string ArchiveExtension = ".apkw";
    private static readonly Encoding Windows1251Encoding;

    private FileStream _archiveStream;
    private ZipArchive _archive;

    public string OpenedArchivePath { get; private set; }
    public IReadOnlyList<string> IntegrityNotifications { get; private set; } = Array.Empty<string>();

    static ArchiveOpeningService()
    {
      Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
      Windows1251Encoding = Encoding.GetEncoding(1251);
    }

    public void Open(string archivePath)
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

      Close();

      _archiveStream = new FileStream(fullArchivePath, FileMode.Open, FileAccess.Read, FileShare.Read);
      _archive = new ZipArchive(_archiveStream, ZipArchiveMode.Read, leaveOpen: false);
      OpenedArchivePath = fullArchivePath;

      IntegrityNotifications = ArchiveManifestService.ValidateArchive(_archive);
      foreach (var notification in IntegrityNotifications)
      {
        Console.WriteLine($"[Archive Integrity] {notification}");
      }
    }

    public IReadOnlyList<string> GetFileList()
    {
      EnsureArchiveIsOpen();

      return _archive.Entries
        .Where(ArchiveManifestService.IsArchiveFileEntry)
        .Select(entry => entry.FullName)
        .OrderBy(filePath => filePath, StringComparer.OrdinalIgnoreCase)
        .ToList();
    }

    public string GetFileText(string archiveEntryName)
    {
      EnsureArchiveIsOpen();

      var normalizedEntryName = NormalizeRequiredEntryName(archiveEntryName, nameof(archiveEntryName));
      var archiveEntry = _archive.Entries.FirstOrDefault(entry =>
          ArchiveManifestService.IsArchiveFileEntry(entry) &&
          ArchiveManifestService.NormalizeEntryName(entry.FullName)
              .Equals(normalizedEntryName, StringComparison.OrdinalIgnoreCase));

      if (archiveEntry == null)
      {
        throw new FileNotFoundException(
            $"File '{normalizedEntryName}' was not found in archive '{OpenedArchivePath}'.",
            normalizedEntryName);
      }

      try
      {
        // 1) Пробуем UTF-8 строго.
        using var stream = archiveEntry.Open();
        using var reader = new StreamReader(
            stream,
            new UTF8Encoding(false, true),
            detectEncodingFromByteOrderMarks: true);

        return reader.ReadToEnd();
      }
      catch (DecoderFallbackException)
      {
        // 2) Поток ZipEntry не поддерживает Position/Seek, открываем заново.
        using var stream = archiveEntry.Open();
        using var reader = new StreamReader(stream, Encoding.GetEncoding(866));
        return reader.ReadToEnd();
      }
    }

    public void Close()
    {
      _archive?.Dispose();
      _archive = null;

      _archiveStream?.Dispose();
      _archiveStream = null;

      OpenedArchivePath = null;
      IntegrityNotifications = Array.Empty<string>();
    }

    public void Dispose()
    {
      Close();
    }

    private void EnsureArchiveIsOpen()
    {
      if (_archive == null)
      {
        throw new InvalidOperationException("Archive is not open. Call Open() first.");
      }
    }

    private static string NormalizeRequiredEntryName(string archiveEntryName, string parameterName)
    {
      if (string.IsNullOrWhiteSpace(archiveEntryName))
      {
        throw new ArgumentException("Archive entry name is required.", parameterName);
      }

      var normalizedName = ArchiveManifestService.NormalizeEntryName(archiveEntryName.Trim());
      if (string.IsNullOrWhiteSpace(normalizedName))
      {
        throw new ArgumentException("Archive entry name cannot be empty.", parameterName);
      }

      if (normalizedName.EndsWith("/", StringComparison.Ordinal))
      {
        throw new ArgumentException("Archive entry name must point to a file, not a directory.", parameterName);
      }

      return normalizedName;
    }
  }
}
