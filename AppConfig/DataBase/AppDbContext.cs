using AppConfig.DataBase.Configurations;
using AppConfig.DataBase.Models;
using Microsoft.EntityFrameworkCore;

namespace AppConfig.DataBase
{
  /// <summary>
  /// Контекст базы данных для управления устройствами.
  /// </summary>
  public class AppDbContext : DbContext
  {
    /// <summary>
    /// Таблица менеджеров шасси.
    /// </summary>
    public DbSet<ChassisManagerEntity> ChassisManagers { get; set; }

    /// <summary>
    /// Таблица модулей коммутации реле.
    /// </summary>
    public DbSet<RelaySwitchModuleEntity> RelaySwitchModules { get; set; }

    /// <summary>
    /// Таблица модулей источников напряжения и тока.
    /// </summary>
    public DbSet<PowerSourceModuleEntity> PowerSourceModules { get; set; }

    /// <summary>
    /// Таблица устройств коммутации.
    /// </summary>
    public DbSet<SwitchingDeviceEntity> SwitchingDevices { get; set; }

    /// <summary>
    /// Таблица точных измерителей.
    /// </summary>
    public DbSet<PrecisionMeterEntity> PrecisionMeters { get; set; }

    /// <summary>
    /// Таблица быстрых измерителей.
    /// </summary>
    public DbSet<FastMeterEntity> FastMeters { get; set; }

    /// <summary>
    /// Таблица пробойных установок.
    /// </summary>
    public DbSet<BreakdownTesterEntity> BreakdownTesters { get; set; }

    /// <summary>
    /// Таблица стоек.
    /// </summary>
    public DbSet<RackEntity> Rack { get; set; }

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
            Path.GetDirectoryName(AppContext.BaseDirectory),
            FileLocations.ConfigFilePath
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
      modelBuilder.ApplyConfiguration(new ChassisManagerConfiguration());
      modelBuilder.ApplyConfiguration(new RelaySwitchModuleConfiguration());
      modelBuilder.ApplyConfiguration(new PowerSourceModuleConfiguration());
      modelBuilder.ApplyConfiguration(new SwitchingDeviceConfiguration());
      modelBuilder.ApplyConfiguration(new BreakdownTesterConfiguration());
      modelBuilder.ApplyConfiguration(new FastMeterConfiguration());
      modelBuilder.ApplyConfiguration(new PrecisionMeterConfiguration());
      modelBuilder.ApplyConfiguration(new RackConfiguration());

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
