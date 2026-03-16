using System.IO;
using System.IO.Compression;
using System.Text;
using static Ask.LogLib.LoggerUtility;

namespace UI.Services.Archive
{
  internal sealed class ArchiveOpeningService : IDisposable
  {
    private const string ArchiveExtension = ".apkw";
    private static readonly Encoding Windows1251Encoding;

    private FileStream _archiveStream;
    private ZipArchive _archive;
    private IDisposable _encryptionSession;

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
        LogError($"Требуется указать путь к архиву");
        throw new ArgumentException("Требуется указать путь к архиву.", nameof(archivePath));
      }

      var fullArchivePath = Path.GetFullPath(archivePath);

      if (!File.Exists(fullArchivePath))
      {
        LogError($"Архив не был найден: {fullArchivePath}");
        throw new FileNotFoundException($"Архив не был найден: {fullArchivePath}", fullArchivePath);
      }

      if (!string.Equals(Path.GetExtension(fullArchivePath), ArchiveExtension, StringComparison.OrdinalIgnoreCase))
      {
        LogError($"Расширение архива не поддерживается.   Ожидалось: {ArchiveExtension}");
        throw new InvalidDataException($"Расширение архива не поддерживается. Ожидалось: {ArchiveExtension}");
      }

      Close();

      IDisposable openedEncryptionSession = null;
      try
      {
        openedEncryptionSession = ArchiveEncryptionSession.Acquire(fullArchivePath);

        _archiveStream = new FileStream(fullArchivePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        _archive = new ZipArchive(_archiveStream, ZipArchiveMode.Read, leaveOpen: false);
        OpenedArchivePath = fullArchivePath;
        _encryptionSession = openedEncryptionSession;
        openedEncryptionSession = null;

        IntegrityNotifications = ArchiveManifestService.ValidateArchive(_archive);
        foreach (var notification in IntegrityNotifications)
        {
          LogError($"[Уведомление о целостности архива] {notification}");
        }
      }
      catch
      {
        _archive?.Dispose();
        _archive = null;

        _archiveStream?.Dispose();
        _archiveStream = null;

        _encryptionSession?.Dispose();
        _encryptionSession = null;
        openedEncryptionSession?.Dispose();

        OpenedArchivePath = null;
        IntegrityNotifications = Array.Empty<string>();
        throw;
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
        LogError($"File '{normalizedEntryName}' was not found in archive '{OpenedArchivePath}'.");
        throw new FileNotFoundException(
            $"File '{normalizedEntryName}' was not found in archive '{OpenedArchivePath}'.",
            normalizedEntryName);

      }

      try
      {
        using var stream = archiveEntry.Open();
        using var reader = new StreamReader(
            stream,
            new UTF8Encoding(false, true),
            detectEncodingFromByteOrderMarks: true);

        return reader.ReadToEnd();
      }
      catch (DecoderFallbackException)
      {
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

      _encryptionSession?.Dispose();
      _encryptionSession = null;

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
        LogError($"Archive is not open. Call Open() first.");
        throw new InvalidOperationException("Archive is not open. Call Open() first.");
      }
    }

    private static string NormalizeRequiredEntryName(string archiveEntryName, string parameterName)
    {
      if (string.IsNullOrWhiteSpace(archiveEntryName))
      {
        LogError($"Требуется указать имя записи в архиве.");
        throw new ArgumentException("Требуется указать имя записи в архиве.", parameterName);
      }

      var normalizedName = ArchiveManifestService.NormalizeEntryName(archiveEntryName.Trim());
      if (string.IsNullOrWhiteSpace(normalizedName))
      {
        LogError($"Название архива не может быть пустым.");
        throw new ArgumentException("Название архива не может быть пустым.", parameterName);
      }

      if (normalizedName.EndsWith("/", StringComparison.Ordinal))
      {
        LogError($"Имя записи в архиве должно указывать на файл, а не на каталог.");
        throw new ArgumentException("Archive entry name must point to a file, not a directory.", parameterName);
      }

      return normalizedName;
    }
  }
}
