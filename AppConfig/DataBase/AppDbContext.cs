using AppConfig.Config;
using AppConfig.DataBase.Models;
using Microsoft.EntityFrameworkCore;

namespace AppConfig.DataBase
{
  /// <summary>
  /// Контекст базы данных для управления устройствами
  /// </summary>
  public class AppDbContext : DbContext
  {
    /// <summary>
    /// Таблица менеджеров шасси
    /// </summary>
    public DbSet<ChassisManagerEntity> ChassisManagers { get; set; }

    /// <summary>
    /// Таблица модулей коммутации реле
    /// </summary>
    public DbSet<RelaySwitchModuleEntity> RelaySwitchModules { get; set; }

    /// <summary>
    /// Таблица модулей источников напряжения и тока
    /// </summary>
    public DbSet<PowerSourceModuleEntity> PowerSourceModules { get; set; }

    /// <summary>
    /// Таблица устройств коммутации
    /// </summary>
    public DbSet<SwitchingDeviceEntity> SwitchingDevices { get; set; }

    /// <summary>
    /// Таблица точных измерителей
    /// </summary>
    public DbSet<PrecisionMeterEntity> PrecisionMeters { get; set; }

    /// <summary>
    /// Таблица быстрых измерителей
    /// </summary>
    public DbSet<FastMeterEntity> FastMeters { get; set; }

    /// <summary>
    /// Таблица пробойных установок
    /// </summary>
    public DbSet<BreakdownTesterEntity> BreakdownTesters { get; set; }

    /// <summary>
    /// Конфигурация базы данных
    /// </summary>
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
      optionsBuilder.UseSqlite($"Data Source={FileLocations.ConfigFilePath}");
    }

    /// <summary>
    /// Настройка моделей базы данных
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
      modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
  }
}
