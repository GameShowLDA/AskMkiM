namespace Ask.DataBase.Provider.Configuration
{
  /// <summary>
  /// Возвращает единый путь к файлу базы данных для всех проектов решения.
  /// База всегда хранится рядом с общей папкой сборки главного приложения.
  /// </summary>
  internal static class DbPathResolver
  {
    /// <summary>
    /// Возвращает полный путь к файлу базы данных и гарантирует существование каталога.
    /// </summary>
    /// <returns>Полный путь к <c>app.db</c>.</returns>
    internal static string Resolve()
    {
      var path = Path.Combine(AppContext.BaseDirectory, "Resources", "app.db");
      path = Path.GetFullPath(path);
      EnsureDirectory(path);
      return path;
    }

    private static void EnsureDirectory(string filePath)
    {
      var directory = Path.GetDirectoryName(filePath);
      if (!string.IsNullOrWhiteSpace(directory))
      {
        Directory.CreateDirectory(directory);
      }
    }
  }
}
