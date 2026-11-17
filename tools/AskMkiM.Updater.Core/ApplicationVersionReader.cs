using System.Diagnostics;
using System.Text.Json;
using AskMkiM.Updater.Core.Models;

namespace AskMkiM.Updater.Core;

/// <summary>
/// Читает версию AskMkiM из манифеста сборки или свойств исполняемого файла.
/// </summary>
public static class ApplicationVersionReader
{
  /// <summary>Имя основного исполняемого файла.</summary>
  public const string ExecutableName = "AskMkiM.exe";

  /// <summary>
  /// Читает сведения о версии из указанного файла AskMkiM.
  /// </summary>
  public static ApplicationVersionInfo Read(string executablePath)
  {
    string fullPath = Path.GetFullPath(executablePath);
    if (!File.Exists(fullPath))
    {
      throw new FileNotFoundException("Исполняемый файл AskMkiM не найден.", fullPath);
    }

    if (!string.Equals(Path.GetFileName(fullPath), ExecutableName, StringComparison.OrdinalIgnoreCase))
    {
      throw new InvalidDataException($"Выберите файл {ExecutableName}.");
    }

    string directory = Path.GetDirectoryName(fullPath)!;
    string manifestPath = Path.Combine(directory, "build-manifest.json");
    if (File.Exists(manifestPath))
    {
      using JsonDocument document = JsonDocument.Parse(File.ReadAllText(manifestPath));
      JsonElement root = document.RootElement;
      string? version = GetString(root, "Version");
      string? buildIdentifier = GetString(root, "BuildIdentifier");
      if (!string.IsNullOrWhiteSpace(version))
      {
        return new ApplicationVersionInfo(fullPath, version, buildIdentifier ?? version);
      }
    }

    FileVersionInfo fileVersion = FileVersionInfo.GetVersionInfo(fullPath);
    string fallbackVersion = fileVersion.ProductVersion
      ?? fileVersion.FileVersion
      ?? throw new InvalidDataException("Не удалось определить версию AskMkiM.");
    return new ApplicationVersionInfo(fullPath, fallbackVersion, fallbackVersion);
  }

  private static string? GetString(JsonElement root, string propertyName)
  {
    foreach (JsonProperty property in root.EnumerateObject())
    {
      if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
      {
        return property.Value.ValueKind == JsonValueKind.String ? property.Value.GetString() : null;
      }
    }

    return null;
  }
}
