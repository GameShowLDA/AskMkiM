using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataBaseConfiguration.Services.Hotkey.Defaults;
using Microsoft.EntityFrameworkCore;

namespace DataBaseConfiguration
{
  static public class DataBaseConfig
  {
    /// <summary>
    /// Путь к файлу настроек конфигурации.
    /// </summary>
    //static public string ConfigFilePath => ".\\Settings\\_config.db";

    /// <summary>
    /// Путь к временной базе данных в системной папке Temp.
    /// </summary>
    static public string ConfigFilePath
    {
      get
      {
        var tempDir = Path.Combine(Path.GetTempPath(), "AskMkiM");
        Directory.CreateDirectory(tempDir);
        string path = Path.Combine(tempDir, "_config.db");
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

        FileHotkeySeeder.Seed(context);

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
