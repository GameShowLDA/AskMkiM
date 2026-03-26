using Ask.DataBase.Provider.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Ask.DataBase.Provider.Context;

/// <summary>
/// Контекст базы данных для управления устройствами.
/// </summary>
public partial class AppDbContext : DbContext
{
  public AppDbContext() { }

  public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

  /// <summary>
  /// Конфигурация базы данных.
  /// </summary>
  /// <param name="optionsBuilder">
  /// Построитель параметров контекста базы данных. Используется для задания параметров подключения.
  /// </param>
  protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
  {
    optionsBuilder.ConfigureWarnings(warnings =>
      warnings.Ignore(RelationalEventId.PendingModelChangesWarning));

    if (!optionsBuilder.IsConfigured)
    {
      optionsBuilder.UseSqlite($"Data Source={DbPathResolver.Resolve()}");
    }
  }
}
