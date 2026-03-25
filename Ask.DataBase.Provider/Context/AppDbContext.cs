using Ask.DataBase.Provider.Configuration;
using Microsoft.EntityFrameworkCore;

namespace Ask.DataBase.Provider.Context
{
  /// <summary>
  /// Контекст базы данных для управления устройствами.
  /// </summary>
  public partial class AppDbContext : DbContext
  {
    /// <summary>
    /// Конфигурация базы данных.
    /// </summary>
    /// <param name="optionsBuilder">
    /// Построитель параметров контекста базы данных. Используется для задания параметров подключения.
    /// </param>
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
      if (optionsBuilder.IsConfigured == false)
      {
        optionsBuilder.UseSqlite($"Data Source={DbPathResolver.Resolve()}");
      }
    }

    /// <summary>
    /// Инициализирует новый экземпляр контекста базы данных <see cref="AppDbContext"/>.
    /// </summary>
    /// <param name="options">
    /// Параметры конфигурации контекста, включая источник данных и поведение подключения.
    /// </param>
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
  }
}
