using Ask.Core.Services.App;
using Ask.Core.Shared.DTO.Settings;
using System.IO;

namespace Ask.Core.Services.FilesUtility;

/// <summary>
/// Хранит последний путь, выбранный пользователем.
/// </summary>
public static class LastDirectoryService
{
  private static readonly string SettingsPath =
    Path.Combine(
      AppDomain.CurrentDomain.BaseDirectory,
      "Settings",
      "fileDialogSettings.yaml");

  /// <summary>
  /// Директория по умолчанию.
  /// </summary>
  private static readonly string DefaultDirectory =
    Path.GetFullPath(
      Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory,
        @"..\Тесты ПК"));

  private static readonly YamlService<FileDialogSettings>
    YamlService = new(SettingsPath);

  /// <summary>
  /// Возвращает последний сохранённый путь.
  /// </summary>
  public static string GetLastDirectory()
  {
    EnsureDefaultDirectoryExists();

    FileDialogSettings settings = YamlService.Load();

    if (!string.IsNullOrWhiteSpace(settings.LastDirectoryPath) &&
        Directory.Exists(settings.LastDirectoryPath))
    {
      return settings.LastDirectoryPath;
    }

    return DefaultDirectory;
  }

  /// <summary>
  /// Сохраняет последний выбранный путь.
  /// </summary>
  public static void SaveLastDirectory(string path)
  {
    if (string.IsNullOrWhiteSpace(path))
    {
      return;
    }

    if (!Directory.Exists(path))
    {
      return;
    }

    FileDialogSettings settings = YamlService.Load();
    settings.LastDirectoryPath = path;

    YamlService.Save(settings);
  }

  /// <summary>
  /// Возвращает до трёх последних существующих файлов без повторов.
  /// </summary>
  /// <returns>Пути файлов, от последнего открытого к более старым.</returns>
  public static IReadOnlyList<string> GetRecentFiles() =>
    (YamlService.Load().RecentFiles ?? new List<string>())
      .Where(File.Exists)
      .Distinct(StringComparer.OrdinalIgnoreCase)
      .Take(3)
      .ToArray();

  /// <summary>
  /// Сохраняет успешно открытый файл в начале истории.
  /// </summary>
  /// <param name="path">Путь открытого файла.</param>
  public static void RememberFile(string path)
  {
    if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
    {
      return;
    }

    FileDialogSettings settings = YamlService.Load();
    settings.RecentFiles = new[] { Path.GetFullPath(path) }
      .Concat(settings.RecentFiles ?? new List<string>())
      .Where(File.Exists)
      .Distinct(StringComparer.OrdinalIgnoreCase)
      .Take(3)
      .ToList();
    try
    {
      YamlService.Save(settings);
    }
    catch (IOException ex)
    {
      Ask.LogLib.LoggerUtility.LogException("Не удалось сохранить историю открытых файлов", ex);
    }
    catch (UnauthorizedAccessException ex)
    {
      Ask.LogLib.LoggerUtility.LogException("Нет доступа к истории открытых файлов", ex);
    }
  }

  /// <summary>
  /// Создаёт директорию по умолчанию, если её нет.
  /// </summary>
  private static void EnsureDefaultDirectoryExists()
  {
    if (!Directory.Exists(DefaultDirectory))
    {
      Directory.CreateDirectory(DefaultDirectory);
    }
  }
}