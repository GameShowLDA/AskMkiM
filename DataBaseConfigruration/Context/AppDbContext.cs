using DataBaseConfiguration.Configurations.Measurement;
using Microsoft.EntityFrameworkCore;

namespace DataBaseConfiguration.Context
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
        string basePath = Path.Combine(
            Path.GetDirectoryName(AppContext.BaseDirectory), DataBaseConfig.ConfigFilePath
        );

        optionsBuilder.UseSqlite($"Data Source={basePath}");
      }
    }

    /// <summary>
    /// Настройка моделей базы данных.
    /// </summary>
    /// <param name="modelBuilder">
    /// Построитель моделей, используемый для конфигурации сущностей и их связей в базе данных.
    /// </param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
      modelBuilder.ApplyConfiguration(new MeasurementErrorEntityConfiguration());
      modelBuilder.ApplyConfiguration(new MeasurementErrorRangeEntityConfiguration());

      base.OnModelCreating(modelBuilder);
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
