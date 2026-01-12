using Ask.Core.Shared.Entity.Devices;
using Microsoft.EntityFrameworkCore;

namespace DataBaseConfiguration.Context
{
  public partial class AppDbContext
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
  }
}
