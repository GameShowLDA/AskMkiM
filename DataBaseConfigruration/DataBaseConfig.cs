using Ask.Core.Services.Translator;
using DataBaseConfiguration.Context;
using DataBaseConfiguration.Services.Hotkey.Defaults;
using DataBaseConfiguration.Services.MeasurementError;
using Microsoft.EntityFrameworkCore;

namespace DataBaseConfiguration
{
  static public class DataBaseConfig
  {
    /// <summary>
    /// Путь к базе данных (_config.db), которая копируется в выходной каталог вместе с программой.
    /// </summary>
    public static string ConfigFilePath
    {
      get
      {
        string baseDir = AppContext.BaseDirectory;
        string path = Path.Combine(baseDir, "Resources", "_config.db");
        return path;
      }
    }

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
        ErrorProviderLocator.Provider = new MeasurementErrorServices();

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("✅ База данных инициализирована, миграции применены.");
        Console.ResetColor();
      }
      catch (Exception ex)
      {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"❌ Ошибка при инициализации базы данных: {ex.Message}");
        Console.ResetColor();
      }
    }

  }
}
