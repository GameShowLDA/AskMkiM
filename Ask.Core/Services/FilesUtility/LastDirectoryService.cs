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

    FileDialogSettings settings = new()
    {
      LastDirectoryPath = path
    };

    YamlService.Save(settings);
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