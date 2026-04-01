using Ask.Core.Services.Translator;
using DataBaseConfiguration.Context;
using DataBaseConfiguration.Services.Hotkey.Defaults;
using DataBaseConfiguration.Services.MeasurementError;
using DataBaseConfiguration.Services.Settings;
using Microsoft.EntityFrameworkCore;

namespace DataBaseConfiguration
{
  static public class DataBaseConfig
  {
    /// <summary>
    /// Путь к базе данных (_config.db), которая копируется в выходной каталог вместе с программой.
    /// </summary>
    public static string ConfigFilePath => ResolveConfigFilePath();

    /// <summary>
    /// Опции конфигурации базы данных для подключения через SQLite.
    /// </summary>
    static internal readonly DbContextOptionsBuilder<AppDbContext> OptionsBuilder = new DbContextOptionsBuilder<AppDbContext>().UseSqlite($"Data Source={ConfigFilePath}");

    /// <summary>
    /// Контекст базы данных, используемый для управления состоянием системы.
    /// </summary>
    static public AppDbContext Context => new AppDbContext(OptionsBuilder.Options);

    /// <summary>
    /// Инициализирует базу данных, применяя все ожидающие миграции.
    /// </summary>
    /// <returns>Задача, представляющая асинхронную операцию инициализации базы данных.</returns>
    static public async Task InitializeDB()
    {
      try
      {
        using var context = new AppDbContext(OptionsBuilder.Options);
        await context.Database.MigrateAsync();
        context.Database.EnsureCreated();

        FileHotkeySeeder.Seed(context);
        MeasurementErrorSeeder.Seed(context);
        RolePasswordSeeder.Seed(context);
        ErrorProviderLocator.Provider = new MeasurementErrorServices();

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"✅ База данных инициализирована, миграции применены. Path: {ConfigFilePath}");
        Console.ResetColor();
      }
      catch (Exception ex)
      {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"❌ Ошибка при инициализации базы данных: {ex.Message}");
        Console.WriteLine($"   Path: {ConfigFilePath}");
        Console.ResetColor();
      }
    }

    private static string ResolveConfigFilePath()
    {
      string localPath = Path.Combine(AppContext.BaseDirectory, "Resources", "_config.db");

      string root = Path.GetPathRoot(AppContext.BaseDirectory) ?? "D:\\";
      string mainWindowPath = Path.Combine(root, "AskMkiM", "Bin", "Resources", "_config.db");

      string? solutionRoot = FindSolutionRoot();
      string solutionBuildPath = solutionRoot == null
        ? string.Empty
        : Path.Combine(solutionRoot, "DataBaseConfigruration", "Bin", "DataBaseConfiguration", "Resources", "_config.db");

      var candidates = new[]
      {
        localPath,
        mainWindowPath,
        solutionBuildPath,
      };

      foreach (string candidate in candidates)
      {
        if (!string.IsNullOrWhiteSpace(candidate) && File.Exists(candidate))
        {
          EnsureParentDirectoryExists(candidate);
          return candidate;
        }
      }

      EnsureParentDirectoryExists(localPath);
      return localPath;
    }

    private static string? FindSolutionRoot()
    {
      DirectoryInfo? directory = new DirectoryInfo(AppContext.BaseDirectory);

      while (directory != null)
      {
        string solutionFile = Path.Combine(directory.FullName, "AskMkiM.sln");
        if (File.Exists(solutionFile))
        {
          return directory.FullName;
        }

        directory = directory.Parent;
      }

      return null;
    }

    private static void EnsureParentDirectoryExists(string filePath)
    {
      string? directoryPath = Path.GetDirectoryName(filePath);
      if (!string.IsNullOrWhiteSpace(directoryPath))
      {
        Directory.CreateDirectory(directoryPath);
      }
    }
  }
}
