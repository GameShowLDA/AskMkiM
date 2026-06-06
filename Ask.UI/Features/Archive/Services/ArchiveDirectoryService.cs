using Ask.Core.Shared.Metadata.Static;
using System.IO;
using static Ask.LogLib.LoggerUtility;

namespace Ask.UI.Features.Archive.Services
{
  /// <summary>
  /// Предоставляет методы для работы с директориями архивов.
  /// </summary>
  public static class ArchiveDirectoryService
  {
    /// <summary>
    /// Имя директории архивов на проверке.
    /// </summary>
    private const string ReviewArchivesDirectoryName = "ConversionReview";

    /// <summary>
    /// Возможные директории размещения архивов.
    /// </summary>
    private static readonly string[] ArchiveFolderCandidates = new[]
    {
      Path.Combine(Directory.GetParent(AppContext.BaseDirectory)!.FullName, FileLocations.ArchiveDirectory),
      Path.Combine(Directory.GetCurrentDirectory(), FileLocations.ArchiveDirectory),
    };

    /// <summary>
    /// Определяет и возвращает путь к корневому каталогу архивов.
    /// </summary>
    /// <returns>
    /// Полный путь к существующему (или созданному) каталогу архивов.
    /// </returns>
    /// <exception cref="DirectoryNotFoundException">
    /// Выбрасывается, если не удалось создать или получить доступ ни к одному из возможных каталогов.
    /// </exception>
    /// <remarks>
    /// Проверяет несколько возможных расположений каталога и создаёт его при необходимости.
    /// Устанавливает атрибут "скрытый" для корневого каталога.
    /// </remarks>
    public static string ResolveArchivesRootPath()
    {
      foreach (var candidatePath in ArchiveFolderCandidates)
      {
        try
        {
          var directoryInfo = Directory.CreateDirectory(candidatePath);
          EnsureRootDirectoryIsHidden(directoryInfo);
          return directoryInfo.FullName;
        }
        catch
        {
        }
      }

      var message = "Не удалось открыть папку архивов.";
      LogError(message);
      throw new DirectoryNotFoundException(message);
    }
    
    /// <summary>
    /// Определяет и возвращает путь к корневому каталогу архивов на проверке.
    /// </summary>
    /// <returns>
    /// Полный путь к существующему (или созданному) каталогу архивов на проверке.
    /// </returns>
    public static string ResolveReviewArchivesRootPath()
    {
      var archivesRootPath = ResolveArchivesRootPath();
      var reviewRootPath = Path.Combine(archivesRootPath, ReviewArchivesDirectoryName);
      return Directory.CreateDirectory(reviewRootPath).FullName;
    }

    /// <summary>
    /// Возвращает список подкаталогов в корневом каталоге архивов.
    /// </summary>
    /// <param name="archivesRootPath">Путь к корневому каталогу архивов.</param>
    /// <returns>
    /// Отсортированный список путей к подкаталогам.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Выбрасывается, если путь не задан.
    /// </exception>
    /// <exception cref="DirectoryNotFoundException">
    /// Выбрасывается, если указанный каталог не существует.
    /// </exception>
    public static IReadOnlyList<string> GetArchiveDirectories(string archivesRootPath)
    {
      EnsureExistingDirectory(archivesRootPath, nameof(archivesRootPath));

      return Directory.EnumerateDirectories(archivesRootPath, "*", SearchOption.TopDirectoryOnly)
        .OrderBy(path => Path.GetFileName(path), StringComparer.OrdinalIgnoreCase)
        .ToList();
    }

    /// <summary>
    /// Возвращает список архивных файлов в указанном каталоге.
    /// </summary>
    /// <param name="directoryPath">Путь к каталогу.</param>
    /// <returns>
    /// Отсортированный список файлов с расширением .apkw.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Выбрасывается, если путь не задан.
    /// </exception>
    /// <exception cref="DirectoryNotFoundException">
    /// Выбрасывается, если каталог не существует.
    /// </exception>
    public static IReadOnlyList<string> GetArchivesInDirectory(string directoryPath)
    {
      EnsureExistingDirectory(directoryPath, nameof(directoryPath));

      return Directory.EnumerateFiles(directoryPath, "*.apkw", SearchOption.TopDirectoryOnly)
        .OrderBy(path => Path.GetFileName(path), StringComparer.OrdinalIgnoreCase)
        .ToList();
    }

    /// <summary>
    /// Возвращает список директорий архивов на проверке.
    /// </summary>
    /// <param name="reviewRootPath">Путь к корневой директории архивов на проверке.</param>
    /// <returns>Список путей к директориям архивов на проверке.</returns>
    public static IReadOnlyList<string> GetReviewDirectories(string reviewRootPath)
    {
      EnsureExistingDirectory(reviewRootPath, nameof(reviewRootPath));

      return Directory.EnumerateDirectories(reviewRootPath, "*", SearchOption.TopDirectoryOnly)
        .OrderBy(path => Path.GetFileName(path), StringComparer.OrdinalIgnoreCase)
        .ToList();
    }

    /// <summary>
    /// Создаёт новый подкаталог в корневом каталоге архивов.
    /// </summary>
    /// <param name="archivesRootPath">Путь к корневому каталогу архивов.</param>
    /// <param name="directoryName">Имя создаваемого каталога.</param>
    /// <returns>
    /// Полный путь к созданному каталогу.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Выбрасывается, если путь или имя каталога некорректны.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Выбрасывается, если каталог уже существует.
    /// </exception>
    /// <exception cref="DirectoryNotFoundException">
    /// Выбрасывается, если корневой каталог не найден.
    /// </exception>
    /// <remarks>
    /// Имя каталога нормализуется перед созданием (удаляются недопустимые символы).
    /// </remarks>
    public static string CreateDirectory(string archivesRootPath, string directoryName)
    {
      var fullArchivesRootPath = EnsureExistingDirectory(archivesRootPath, nameof(archivesRootPath));
      var normalizedDirectoryName = NormalizeDirectoryName(directoryName);
      var directoryPath = Path.Combine(fullArchivesRootPath, normalizedDirectoryName);

      if (Directory.Exists(directoryPath))
      {
        var message = $"Каталог '{normalizedDirectoryName}' уже существует.";
        LogError(message);
        throw new InvalidOperationException(message);
      }

      return Directory.CreateDirectory(directoryPath).FullName;
    }

    /// <summary>
    /// Нормализует имя каталога, удаляя недопустимые символы и приводя его к корректному виду.
    /// </summary>
    /// <param name="directoryName">Исходное имя каталога.</param>
    /// <returns>
    /// Корректное имя каталога, пригодное для использования в файловой системе.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Выбрасывается, если имя пустое или содержит только недопустимые символы.
    /// </exception>
    public static string NormalizeDirectoryName(string directoryName)
    {
      if (string.IsNullOrWhiteSpace(directoryName))
      {
        var message = "Название каталога обязательно.";
        LogError(message);
        throw new ArgumentException(message, nameof(directoryName));
      }

      var normalizedName = Path.GetFileName(directoryName.Trim());
      foreach (var invalidChar in Path.GetInvalidFileNameChars())
      {
        normalizedName = normalizedName.Replace(invalidChar, '_');
      }

      if (string.IsNullOrWhiteSpace(normalizedName))
      {
        var message = "Название каталога содержит только недопустимые символы.";
        LogError(message);
        throw new ArgumentException(message, nameof(directoryName));
      }

      return normalizedName;
    }

    /// <summary>
    /// Проверяет существование каталога и возвращает его полный путь.
    /// </summary>
    /// <param name="directoryPath">Путь к каталогу.</param>
    /// <param name="parameterName">Имя параметра для формирования исключения.</param>
    /// <returns>
    /// Нормализованный полный путь к существующему каталогу.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Выбрасывается, если путь пустой или не задан.
    /// </exception>
    /// <exception cref="DirectoryNotFoundException">
    /// Выбрасывается, если каталог не существует.
    /// </exception>
    public static string EnsureExistingDirectory(string directoryPath, string parameterName)
    {
      if (string.IsNullOrWhiteSpace(directoryPath))
      {
        var message = "Не удалось определить каталог архивов.";
        LogError(message);
        throw new ArgumentException(message, parameterName);
      }

      var fullDirectoryPath = Path.GetFullPath(directoryPath);
      if (!Directory.Exists(fullDirectoryPath))
      {
        var message = $"Каталог архивов не найден: {fullDirectoryPath}";
        LogError(message);
        throw new DirectoryNotFoundException(message);
      }

      return fullDirectoryPath;
    }

    /// <summary>
    /// Устанавливает атрибут скрытой директории для корневой папки, если он отсутствует.
    /// </summary>
    /// <param name="directoryInfo">Информация о директории.</param>
    private static void EnsureRootDirectoryIsHidden(DirectoryInfo directoryInfo)
    {
      if ((directoryInfo.Attributes & FileAttributes.Hidden) == 0)
      {
        directoryInfo.Attributes |= FileAttributes.Hidden;
      }
    }
  }
}
