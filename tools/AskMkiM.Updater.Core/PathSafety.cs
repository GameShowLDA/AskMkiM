namespace AskMkiM.Updater.Core;

/// <summary>
/// Выполняет безопасную нормализацию путей пакета обновления.
/// </summary>
public static class PathSafety
{
  /// <summary>
  /// Нормализует относительный путь и запрещает выход из корневого каталога.
  /// </summary>
  /// <param name="relativePath">Проверяемый относительный путь.</param>
  /// <returns>Нормализованный путь с разделителем <c>/</c>.</returns>
  public static string NormalizeRelativePath(string relativePath)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);

    string normalized = relativePath.Replace('\\', '/').TrimStart('/');
    if (Path.IsPathRooted(relativePath)
        || normalized.Split('/').Any(part => part is ".." or "." or ""))
    {
      throw new InvalidDataException($"Недопустимый относительный путь: {relativePath}");
    }

    return normalized;
  }

  /// <summary>
  /// Возвращает безопасный полный путь внутри указанного корневого каталога.
  /// </summary>
  public static string CombineInsideRoot(string rootDirectory, string relativePath)
  {
    string normalizedRoot = Path.GetFullPath(rootDirectory)
      .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
      + Path.DirectorySeparatorChar;
    string normalizedRelative = NormalizeRelativePath(relativePath)
      .Replace('/', Path.DirectorySeparatorChar);
    string result = Path.GetFullPath(Path.Combine(normalizedRoot, normalizedRelative));

    if (!result.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
    {
      throw new InvalidDataException($"Путь выходит из каталога установки: {relativePath}");
    }

    return result;
  }
}
