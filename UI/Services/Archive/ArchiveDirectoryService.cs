using Ask.Core.Shared.Metadata.Static;
using System.IO;
using static Ask.LogLib.LoggerUtility;

namespace UI.Services.Archive
{
  internal static class ArchiveDirectoryService
  {
    private static readonly string[] ArchiveFolderCandidates = new[]
    {
      Path.Combine(AppContext.BaseDirectory, FileLocations.ArchiveDirectory),
      Path.Combine(Directory.GetCurrentDirectory(), FileLocations.ArchiveDirectory),
    };

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

    public static IReadOnlyList<string> GetArchiveDirectories(string archivesRootPath)
    {
      EnsureExistingDirectory(archivesRootPath, nameof(archivesRootPath));

      return Directory.EnumerateDirectories(archivesRootPath, "*", SearchOption.TopDirectoryOnly)
        .OrderBy(path => Path.GetFileName(path), StringComparer.OrdinalIgnoreCase)
        .ToList();
    }

    public static IReadOnlyList<string> GetArchivesInDirectory(string directoryPath)
    {
      EnsureExistingDirectory(directoryPath, nameof(directoryPath));

      return Directory.EnumerateFiles(directoryPath, "*.apkw", SearchOption.TopDirectoryOnly)
        .OrderBy(path => Path.GetFileName(path), StringComparer.OrdinalIgnoreCase)
        .ToList();
    }

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

    private static void EnsureRootDirectoryIsHidden(DirectoryInfo directoryInfo)
    {
      if ((directoryInfo.Attributes & FileAttributes.Hidden) == 0)
      {
        directoryInfo.Attributes |= FileAttributes.Hidden;
      }
    }
  }
}