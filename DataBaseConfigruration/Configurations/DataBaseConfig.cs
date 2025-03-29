using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace DataBaseConfiguration.Configurations
{
  static public class DataBaseConfig
  {
    /// <summary>
    /// Путь к файлу настроек конфигурации.
    /// </summary>
    static public string ConfigFilePath => ".\\Settings\\_config.db";

    /// <summary>
    /// Опции конфигурации базы данных для подключения через SQLite.
    /// </summary>
    static internal readonly DbContextOptionsBuilder<AppDbContext> OptionsBuilder = new DbContextOptionsBuilder<AppDbContext>().UseSqlite($"Data Source={DataBaseConfig.ConfigFilePath}");

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
      var dbContextFactory = new DbContextFactory();
      using (var scope = dbContextFactory.CreateDbContext(new string[0]))
      {
        await scope.Database.MigrateAsync();
      }
    }
  }
}
