namespace AskMkiM.Updater.Core;

/// <summary>
/// Защищает изменяемые данные установки от включения в пакет и замены.
/// </summary>
public static class ProtectedPathPolicy
{
  private static readonly HashSet<string> ProtectedFiles = new(StringComparer.OrdinalIgnoreCase)
  {
    "Resources/app.db",
    "Resources/role-auth.json",
  };

  private static readonly string[] ProtectedDirectories =
  {
    "Settings/",
    "Archives/",
    "History/",
    "Backup/",
    "Saves/",
    "logs/",
    "CrashReports/",
    "UpdateBackups/",
  };

  /// <summary>
  /// Проверяет, относится ли относительный путь к изменяемым данным установки.
  /// </summary>
  /// <param name="relativePath">Путь относительно каталога AskMkiM.</param>
  /// <returns><see langword="true"/>, если путь запрещено обновлять.</returns>
  public static bool IsProtected(string relativePath)
  {
    string normalized = PathSafety.NormalizeRelativePath(relativePath);
    if (ProtectedFiles.Contains(normalized))
    {
      return true;
    }

    return ProtectedDirectories.Any(directory =>
      normalized.StartsWith(directory, StringComparison.OrdinalIgnoreCase));
  }
}
