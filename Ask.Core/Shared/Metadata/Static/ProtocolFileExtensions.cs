namespace Ask.Core.Shared.Metadata.Static;

/// <summary>
/// Расширения файлов протоколов.
/// </summary>
public static class ProtocolFileExtensions
{
  /// <summary>
  /// Протокол выполнения.
  /// </summary>
  public const string Trace = ".asktrace";

  /// <summary>
  /// Итоговый протокол теста.
  /// </summary>
  public const string Result = ".askresult";

  /// <summary>
  /// Итоговый протокол выполнения программы контроля.
  /// </summary>
  public const string Report = ".askreport";

  /// <summary>
  /// Устаревшее расширение протокола выполнения.
  /// </summary>
  public const string LegacyTrace = ".lst";

  /// <summary>
  /// Устаревшее расширение протокола выполнения в кодировке UTF-8.
  /// </summary>
  public const string LegacyUtf8Trace = ".lstw";

  /// <summary>
  /// Устаревшее расширение итогового протокола.
  /// </summary>
  public const string LegacyResult = ".rtlst";

  /// <summary>
  /// Проверяет, соответствует ли расширение протоколу выполнения.
  /// </summary>
  /// <param name="extension">Расширение файла.</param>
  /// <returns>
  /// <see langword="true"/>, если расширение соответствует протоколу выполнения.
  /// В противном случае — <see langword="false"/>.
  /// </returns>
  public static bool IsTrace(string? extension)
    => Equals(extension, Trace)
       || Equals(extension, LegacyTrace)
       || Equals(extension, LegacyUtf8Trace);

  /// <summary>
  /// Проверяет, соответствует ли расширение итоговому протоколу.
  /// </summary>
  /// <param name="extension">Расширение файла.</param>
  /// <returns>
  /// <see langword="true"/>, если расширение соответствует итоговому протоколу.
  /// В противном случае — <see langword="false"/>.
  /// </returns>
  public static bool IsSummary(string? extension)
    => Equals(extension, Result)
       || Equals(extension, Report)
       || Equals(extension, LegacyResult);

  /// <summary>
  /// Проверяет, соответствует ли расширение файлу протокола.
  /// </summary>
  /// <param name="extension">Расширение файла.</param>
  /// <returns>
  /// <see langword="true"/>, если расширение соответствует файлу протокола.
  /// В противном случае — <see langword="false"/>.
  /// </returns>
  public static bool IsProtocol(string? extension) => IsTrace(extension) || IsSummary(extension);

  private static bool Equals(string? extension, string expected)
    => string.Equals(extension, expected, StringComparison.OrdinalIgnoreCase);
}
