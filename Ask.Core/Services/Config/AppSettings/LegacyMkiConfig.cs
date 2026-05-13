using System;
using System.IO;
using System.Text.Json;

namespace Ask.Core.Services.Config.AppSettings
{
  /// <summary>
  /// Хранит настройки интеграции со старой программой АСК-МКИ.
  /// </summary>
  public static class LegacyMkiConfig
  {
    private static readonly string ConfigDirectory = Path.Combine(
      Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
      "ASK-MKI-M");

    private static readonly string ConfigFilePath = Path.Combine(
      ConfigDirectory,
      "legacy-mki.json");

    /// <summary>
    /// Возвращает сохранённый путь к mkiw.exe.
    /// </summary>
    /// <returns>Путь к mkiw.exe или пустая строка, если путь не задан.</returns>
    public static string GetMkiPath()
    {
      try
      {
        if (!File.Exists(ConfigFilePath))
        {
          return string.Empty;
        }

        var json = File.ReadAllText(ConfigFilePath);
        var config = JsonSerializer.Deserialize<LegacyMkiSettings>(json);

        return config?.MkiPath ?? string.Empty;
      }
      catch
      {
        return string.Empty;
      }
    }

    /// <summary>
    /// Сохраняет путь к mkiw.exe.
    /// </summary>
    /// <param name="mkiPath">Полный путь к mkiw.exe.</param>
    public static void SetMkiPath(string mkiPath)
    {
      Directory.CreateDirectory(ConfigDirectory);

      var config = new LegacyMkiSettings
      {
        MkiPath = mkiPath ?? string.Empty
      };

      var json = JsonSerializer.Serialize(
        config,
        new JsonSerializerOptions
        {
          WriteIndented = true
        });

      File.WriteAllText(ConfigFilePath, json);
    }

    /// <summary>
    /// Очищает сохранённый путь к mkiw.exe.
    /// </summary>
    public static void ClearMkiPath()
    {
      SetMkiPath(string.Empty);
    }

    /// <summary>
    /// Модель файла настроек старой программы АСК-МКИ.
    /// </summary>
    private sealed class LegacyMkiSettings
    {
      /// <summary>
      /// Полный путь к mkiw.exe.
      /// </summary>
      public string MkiPath { get; set; } = string.Empty;
    }
  }
}