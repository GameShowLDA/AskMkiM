using System.IO;
using System.IO.Compression;
using System.Text;
using static Ask.LogLib.LoggerUtility;

namespace Ask.UI.Features.Archive.Services
{
  /// <summary>
  /// Обеспечивает открытие, чтение и управление состоянием архива.
  /// </summary>
  internal sealed class ArchiveOpeningService : IDisposable
  {
    /// <summary>
    /// Расширение файлов архивов APKW.
    /// </summary>
    private const string ArchiveExtension = ".apkw";

    /// <summary>
    /// Кодировка Windows-1251 для чтения текстовых файлов архива.
    /// </summary>
    private static readonly Encoding Windows1251Encoding;

    /// <summary>
    /// Поток открытого архива.
    /// </summary>
    private FileStream _archiveStream;

    /// <summary>
    /// Текущий открытый ZIP-архив.
    /// </summary>
    private ZipArchive _archive;

    /// <summary>
    /// Сессия шифрования архива.
    /// </summary>
    private IDisposable _encryptionSession;

    /// <summary>
    /// Путь к текущему открытому архиву.
    /// </summary>
    public string OpenedArchivePath { get; private set; }

    /// <summary>
    /// Список уведомлений о нарушениях целостности архива.
    /// </summary>
    public IReadOnlyList<string> IntegrityNotifications { get; private set; } = Array.Empty<string>();

    /// <summary>
    /// Инициализирует статические ресурсы сервиса открытия архивов.
    /// </summary>
    static ArchiveOpeningService()
    {
      Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
      Windows1251Encoding = Encoding.GetEncoding(1251);
    }

    /// <summary>
    /// Открывает архив, выполняет проверку целостности и подготавливает ресурсы для чтения.
    /// </summary>
    /// <param name="archivePath">Путь к архиву.</param>
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
        LogError($"Расширение архива не поддерживается. Ожидалось: {ArchiveExtension}");
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

    /// <summary>
    /// Возвращает список файлов текущего открытого архива.
    /// </summary>
    /// <returns>Список путей файлов внутри архива.</returns>
    public IReadOnlyList<string> GetFileList()
    {
      EnsureArchiveIsOpen();

      return _archive.Entries
        .Where(ArchiveManifestService.IsArchiveFileEntry)
        .Select(entry => entry.FullName)
        .OrderBy(filePath => filePath, StringComparer.OrdinalIgnoreCase)
        .ToList();
    }

    /// <summary>
    /// Считывает текстовое содержимое файла из открытого архива.
    /// </summary>
    /// <param name="archiveEntryName">Имя записи архива.</param>
    /// <returns>Текстовое содержимое файла.</returns>
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

    /// <summary>
    /// Закрывает архив и освобождает связанные ресурсы.
    /// </summary>
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

    /// <summary>
    /// Освобождает ресурсы сервиса открытия архивов.
    /// </summary>
    public void Dispose()
    {
      Close();
    }

    /// <summary>
    /// Проверяет, что архив открыт для работы.
    /// </summary>
    private void EnsureArchiveIsOpen()
    {
      if (_archive == null)
      {
        LogError($"Archive is not open. Call Open() first.");
        throw new InvalidOperationException("Archive is not open. Call Open() first.");
      }
    }

    /// <summary>
    /// Проверяет и нормализует обязательное имя записи архива.
    /// </summary>
    /// <param name="archiveEntryName">Имя записи архива.</param>
    /// <param name="parameterName">Имя параметра для исключения.</param>
    /// <returns>Нормализованное имя записи архива.</returns>
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
