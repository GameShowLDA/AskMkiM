using Ask.Core.Services.FilesUtility;
using Ask.Core.Shared.Metadata.Static;
using System.IO;
using System.Text;
using Ude;

namespace UI.Services
{
  /// <summary>
  /// Предоставляет методы для автоматического определения кодировки текстовых файлов.
  /// </summary>
  /// <remarks>
  /// При чтении редактор учитывает BOM, проверяет UTF-8 и сравнивает его с CP866.
  /// Методы определения кодировки через Ude доступны для файлов из неизвестных источников.
  /// </remarks>
  public static class EncodingService
  {
    private static readonly Encoding Utf8WithoutBom = new UTF8Encoding(false);
    private static readonly Encoding StrictUtf8 = new UTF8Encoding(false, true);
    private static readonly Encoding[] BomEncodings =
    [
      Encoding.UTF32, new UTF32Encoding(true, true), Encoding.UTF8, Encoding.Unicode, Encoding.BigEndianUnicode
    ];

    internal enum DetectionReason
    {
      ByteOrderMark,
      InvalidUtf8,
      Backup,
      TextHeuristic,
      FormatDefault
    }

    /// <summary>
    /// Читает текстовый файл и определяет его кодировку.
    /// </summary>
    /// <param name="path">Путь к текстовому файлу.</param>
    /// <param name="formatDefault">Кодировка формата на случай неоднозначности.</param>
    /// <param name="encrypted">Признак зашифрованного протокола.</param>
    /// <returns>Текст и выбранная кодировка.</returns>
    public static (string Text, Encoding Encoding) ReadFile(string path, Encoding formatDefault, bool encrypted)
    {
      byte[] bytes = encrypted ? FileEncryptionManager.ReadEncryptedFileBytes(path) : File.ReadAllBytes(path);
      var directory = Path.GetDirectoryName(Path.GetFullPath(path));
      var backupDirectory = Path.GetFullPath(FileLocations.BackupDirectory);
      var (text, encoding, _) = Decode(
        bytes, formatDefault, string.Equals(directory, backupDirectory, StringComparison.OrdinalIgnoreCase));
      return (text, encoding);
    }

    internal static (string Text, Encoding Encoding, DetectionReason Reason) Decode(
      ReadOnlySpan<byte> bytes, Encoding formatDefault, bool isBackup = false)
    {
      foreach (var encoding in BomEncodings)
      {
        if (bytes.StartsWith(encoding.Preamble))
        {
          return (encoding.GetString(bytes[encoding.Preamble.Length..]), encoding, DetectionReason.ByteOrderMark);
        }
      }

      Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
      var cp866 = Encoding.GetEncoding(866);
      string utf8Text;
      try
      {
        utf8Text = StrictUtf8.GetString(bytes);
      }
      catch (DecoderFallbackException)
      {
        return (cp866.GetString(bytes), cp866, DetectionReason.InvalidUtf8);
      }

      if (isBackup)
      {
        return (utf8Text, Utf8WithoutBom, DetectionReason.Backup);
      }

      var cp866Text = cp866.GetString(bytes);
      if (utf8Text != cp866Text)
      {
        if (!utf8Text.Any(IsSuspiciousCharacter)
          && cp866Text.Any(IsSuspiciousCharacter)
          && utf8Text.Where(c => c > 127 && char.IsLetter(c)).Take(2).Count() == 2)
        {
          return (utf8Text, Utf8WithoutBom, DetectionReason.TextHeuristic);
        }
      }

      return formatDefault.CodePage == cp866.CodePage
        ? (cp866Text, cp866, DetectionReason.FormatDefault)
        : (utf8Text, Utf8WithoutBom, DetectionReason.FormatDefault);
    }

    private static bool IsSuspiciousCharacter(char c)
      => c is >= '\u2500' and <= '\u259F'
        || (char.IsControl(c) && c is not '\r' and not '\n' and not '\t');

    /// <summary>
    /// Определяет кодировку указанного текстового файла.
    /// </summary>
    /// <param name="filePath">Полный путь к файлу, кодировку которого необходимо определить.</param>
    /// <returns>
    /// Объект <see cref="Encoding"/>, соответствующий определённой кодировке файла.
    /// Если кодировка не может быть определена или не поддерживается, возвращается <see cref="Encoding.UTF8"/>.
    /// </returns>
    /// <exception cref="FileNotFoundException">Выбрасывается, если файл по указанному пути не существует.</exception>
    /// <exception cref="UnauthorizedAccessException">Выбрасывается, если нет прав на чтение файла.</exception>
    public static Encoding DetectEncodingFromFile(string filePath)
    {
      using var fileStream = File.OpenRead(filePath);
      return DetectEncodingFromText(fileStream);
    }

    /// <summary>
    /// Определяет кодировку текста на основе содержимого потока файла.
    /// </summary>
    /// <param name="fileStream">Поток <see cref="FileStream"/>, содержащий текстовые данные.</param>
    /// <returns>
    /// Объект <see cref="Encoding"/>, соответствующий определённой кодировке текста.
    /// Если кодировка не может быть определена или не поддерживается, возвращается <see cref="Encoding.UTF8"/>.
    /// </returns>
    /// <remarks>
    /// Метод не изменяет позицию потока после завершения работы.
    /// Перед вызовом рекомендуется убедиться, что поток поддерживает чтение.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Выбрасывается, если <paramref name="fileStream"/> равен <c>null</c>.</exception>
    public static Encoding DetectEncodingFromText(FileStream fileStream)
    {
      if (fileStream == null)
        throw new ArgumentNullException(nameof(fileStream));

      var detector = new CharsetDetector();
      detector.Feed(fileStream);
      detector.DataEnd();

      if (detector.Charset != null)
      {
        try
        {
          var enc = Encoding.GetEncoding(detector.Charset);
          return enc; 
        }
        catch
        {
          return Encoding.GetEncoding(866);
        }
      }

      return Encoding.UTF8;
    }
  }
}
