using System.IO;

namespace Ask.Core.Shared.Metadata.Enums.FileEnums
{
  /// <summary>
  /// Централизованно сопоставляет расширения файлов с логическими типами,
  /// используемыми редактором и архивным preview.
  /// </summary>
  public static class FileTypeResolver
  {
    /// <summary>
    /// Определяет тип файла по пути или имени файла.
    /// </summary>
    /// <param name="path">Путь, имя файла или имя записи архива.</param>
    /// <returns>Определённый тип файла либо <see cref="FileType.None"/>.</returns>
    public static FileType DetermineFromPath(string? path)
    {
      if (string.IsNullOrWhiteSpace(path))
      {
        return FileType.None;
      }

      return DetermineFromExtension(Path.GetExtension(path));
    }

    /// <summary>
    /// Определяет тип файла по расширению.
    /// </summary>
    /// <param name="extension">Расширение файла.</param>
    /// <returns>Определённый тип файла либо <see cref="FileType.None"/>.</returns>
    public static FileType DetermineFromExtension(string? extension)
    {
      if (string.IsNullOrWhiteSpace(extension))
      {
        return FileType.None;
      }

      return extension.ToLowerInvariant() switch
      {
        ".pk" => FileType.PK,
        ".pkw" => FileType.PKW,
        ".acs" => FileType.PK,
        ".opk" => FileType.OPK,
        ".opkw" => FileType.OPKW,
        ".lst" => FileType.Protocol,
        ".lstw" => FileType.Protocol,
        _ => FileType.None,
      };
    }

    /// <summary>
    /// Возвращает имя XSHD-ресурса для типа файла.
    /// </summary>
    /// <param name="fileType">Тип файла.</param>
    /// <returns>Имя XSHD-файла или <see langword="null"/>, если подсветка не поддерживается.</returns>
    public static string? GetHighlightingResourceName(FileType fileType)
    {
      return fileType switch
      {
        FileType.OPK or FileType.OPKW => "MKI_OPKW.xshd",
        FileType.PK or FileType.PKW => "MKI_PK.xshd",
        FileType.Protocol => "MKI_PROTOCOL.xshd",
        _ => null,
      };
    }

    /// <summary>
    /// Возвращает признак того, что текстовый формат должен читаться и записываться в UTF-8.
    /// </summary>
    /// <param name="fileType">Тип файла.</param>
    /// <returns><see langword="true"/>, если для типа ожидается UTF-8.</returns>
    public static bool UsesUtf8Encoding(FileType fileType)
      => fileType is FileType.PKW or FileType.OPKW or FileType.Protocol;

    /// <summary>
    /// Возвращает признак того, что для типа поддерживается folding OPK/OPKW-структур.
    /// </summary>
    /// <param name="fileType">Тип файла.</param>
    /// <returns><see langword="true"/>, если folding нужно включать.</returns>
    public static bool SupportsOpkFolding(FileType fileType)
      => fileType is FileType.OPK or FileType.OPKW;

    /// <summary>
    /// Возвращает признак того, что для типа нужно включать кастомную подсветку комментариев.
    /// </summary>
    /// <param name="fileType">Тип файла.</param>
    /// <returns><see langword="true"/>, если тип использует синтаксис MKI-комментариев.</returns>
    public static bool SupportsBraceCommentColorizer(FileType fileType)
      => fileType is FileType.PK or FileType.PKW or FileType.OPK or FileType.OPKW;
  }
}
