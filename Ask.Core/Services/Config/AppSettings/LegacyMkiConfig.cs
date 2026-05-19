using System;
using System.IO;
using System.Text.Json;
using Ask.Core.Services.Config.LegacyMki;

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
      return LoadSettings().MkiPath;
    }

    /// <summary>
    /// Сохраняет путь к mkiw.exe.
    /// </summary>
    /// <param name="mkiPath">Полный путь к mkiw.exe.</param>
    public static void SetMkiPath(string mkiPath)
    {
      var config = LoadSettings();
      config.MkiPath = mkiPath ?? string.Empty;

      if (string.IsNullOrWhiteSpace(config.ConfigPath))
      {
        config.ConfigPath = ResolveConfigPathFromExecutable(config.MkiPath);
      }

      SaveSettings(config);
    }

    /// <summary>
    /// Очищает сохранённый путь к mkiw.exe.
    /// </summary>
    public static void ClearMkiPath()
    {
      var config = LoadSettings();
      config.MkiPath = string.Empty;
      SaveSettings(config);
    }

    /// <summary>
    /// Возвращает сохранённый путь к mki_hrd.cfg.
    /// </summary>
    public static string GetConfigPath()
    {
      var config = LoadSettings();
      return !string.IsNullOrWhiteSpace(config.ConfigPath)
        ? config.ConfigPath
        : ResolveConfigPathFromExecutable(config.MkiPath);
    }

    /// <summary>
    /// Сохраняет путь к mki_hrd.cfg.
    /// </summary>
    public static void SetConfigPath(string configPath)
    {
      var config = LoadSettings();
      config.ConfigPath = configPath ?? string.Empty;
      SaveSettings(config);
    }

    /// <summary>
    /// Возвращает выбранный профиль legacy-конфигурации.
    /// </summary>
    public static LegacyMkiProfileKind GetSelectedProfile()
    {
      var config = LoadSettings();
      return Enum.TryParse<LegacyMkiProfileKind>(config.SelectedProfile, true, out var profile)
        ? profile
        : LegacyMkiProfileKind.M1;
    }

    /// <summary>
    /// Сохраняет выбранный профиль legacy-конфигурации.
    /// </summary>
    public static void SetSelectedProfile(LegacyMkiProfileKind profile)
    {
      var config = LoadSettings();
      config.SelectedProfile = profile.ToString();
      SaveSettings(config);
    }

    private static LegacyMkiSettings LoadSettings()
    {
      try
      {
        if (!File.Exists(ConfigFilePath))
        {
          return new LegacyMkiSettings();
        }

        var json = File.ReadAllText(ConfigFilePath);
        return JsonSerializer.Deserialize<LegacyMkiSettings>(json) ?? new LegacyMkiSettings();
      }
      catch
      {
        return new LegacyMkiSettings();
      }
    }

    private static void SaveSettings(LegacyMkiSettings config)
    {
      Directory.CreateDirectory(ConfigDirectory);

      var json = JsonSerializer.Serialize(
        config,
        new JsonSerializerOptions
        {
          WriteIndented = true
        });

      File.WriteAllText(ConfigFilePath, json);
    }

    private static string ResolveConfigPathFromExecutable(string? mkiPath)
    {
      if (string.IsNullOrWhiteSpace(mkiPath))
      {
        return string.Empty;
      }

      try
      {
        var directory = Path.GetDirectoryName(mkiPath);
        return string.IsNullOrWhiteSpace(directory)
          ? string.Empty
          : Path.Combine(directory, "mki_hrd.cfg");
      }
      catch
      {
        return string.Empty;
      }
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

      /// <summary>
      /// Полный путь к mki_hrd.cfg.
      /// </summary>
      public string ConfigPath { get; set; } = string.Empty;

      /// <summary>
      /// Выбранный профиль legacy-конфигурации.
      /// </summary>
      public string SelectedProfile { get; set; } = LegacyMkiProfileKind.M1.ToString();
    }
  }
}
