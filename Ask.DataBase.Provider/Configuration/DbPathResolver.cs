using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ask.DataBase.Provider.Configuration
{
  /// <summary>
  /// Определяет корректный путь к файлу базы данных (app.db),
  /// перебирая набор возможных расположений (локальный каталог приложения,
  /// путь основного приложения, путь относительно решения).
  /// Возвращает первый найденный путь или путь по умолчанию,
  /// гарантируя существование директории.
  /// </summary>
  internal static class DbPathResolver
  {
    /// <summary>
    /// Возвращает путь к файлу базы данных (app.db), проверяя возможные расположения.
    /// При отсутствии файла использует путь по умолчанию и создаёт директорию при необходимости.
    /// </summary>
    /// <returns>Полный путь к файлу базы данных.</returns>
    internal static string Resolve()
    {
      var candidates = GetCandidatePaths();

      var existing = FindExistingPath(candidates);
      if (existing != null)
        return EnsureDirectoryAndReturn(existing);

      return EnsureDirectoryAndReturn(GetDefaultPath());
    }

    /// <summary>
    /// Формирует список возможных путей к файлу базы данных,
    /// включая локальный каталог приложения, путь основного приложения
    /// и путь относительно корня решения.
    /// </summary>
    /// <returns>Коллекция кандидатных путей к файлу базы данных.</returns>
    private static IEnumerable<string> GetCandidatePaths()
    {
      string localPath = Path.Combine(AppContext.BaseDirectory, "Resources", "app.db");

      string root = Path.GetPathRoot(AppContext.BaseDirectory) ?? "D:\\";
      string mainWindowPath = Path.Combine(root, "AskMkiM", "Bin", "Resources", "app.db");

      string? solutionRoot = FindSolutionRoot();
      string solutionBuildPath = solutionRoot == null
          ? string.Empty
          : Path.Combine(solutionRoot, "DataBaseConfigruration", "Bin", "DataBaseConfiguration", "Resources", "app.db");

      return new[] { localPath, mainWindowPath, solutionBuildPath };
    }

    /// <summary>
    /// Возвращает первый существующий путь из переданного набора.
    /// </summary>
    /// <param name="paths">Набор путей для проверки.</param>
    /// <returns>Существующий путь или null, если ни один не найден.</returns>
    private static string? FindExistingPath(IEnumerable<string> paths)
    {
      return paths.FirstOrDefault(p =>
          !string.IsNullOrWhiteSpace(p) && File.Exists(p));
    }

    /// <summary>
    /// Возвращает путь по умолчанию к файлу базы данных
    /// в каталоге приложения.
    /// </summary>
    /// <returns>Путь к файлу базы данных.</returns>
    private static string GetDefaultPath()
    {
      return Path.Combine(AppContext.BaseDirectory, "Resources", "app.db");
    }

    /// <summary>
    /// Обеспечивает существование директории для указанного пути
    /// и возвращает сам путь без изменений.
    /// </summary>
    /// <param name="path">Путь к файлу базы данных.</param>
    /// <returns>Тот же путь после проверки директории.</returns>
    private static string EnsureDirectoryAndReturn(string path)
    {
      EnsureDirectory(path);
      return path;
    }

    /// <summary>
    /// Выполняет поиск корневой директории решения, поднимаясь вверх по дереву каталогов
    /// до обнаружения файла решения (AskMkiM.sln).
    /// </summary>
    /// <returns>Путь к корню решения или null, если файл не найден.</returns>
    private static string? FindSolutionRoot()
    {
      DirectoryInfo? dir = new(AppContext.BaseDirectory);

      while (dir != null)
      {
        if (File.Exists(Path.Combine(dir.FullName, "AskMkiM.sln")))
          return dir.FullName;

        dir = dir.Parent;
      }

      return null;
    }

    /// <summary>
    /// Обеспечивает существование директории для указанного файла,
    /// создавая её при необходимости.
    /// </summary>
    /// <param name="filePath">Путь к файлу, для которого требуется директория.</param>
    private static void EnsureDirectory(string filePath)
    {
      var dir = Path.GetDirectoryName(filePath);
      if (!string.IsNullOrWhiteSpace(dir))
        Directory.CreateDirectory(dir);
    }
  }
}
