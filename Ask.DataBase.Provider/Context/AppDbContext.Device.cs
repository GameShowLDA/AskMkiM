using Ask.Core.Shared.DTO.Devices.Breakdown;
using Ask.Core.Shared.DTO.Devices.ChassisManager;
using Ask.Core.Shared.DTO.Devices.PowerSourceModule;
using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.DataBase.Provider.Contracts.DTO;
using Microsoft.EntityFrameworkCore;

namespace Ask.DataBase.Provider.Context
{
  public partial class AppDbContext
  {
    /// <summary>
    /// Таблица менеджеров шасси.
    /// </summary>
    public DbSet<ChassisManagerDto> ChassisManagers { get; set; }

    /// <summary>
    /// Таблица модулей коммутации реле.
    /// </summary>
    public DbSet<RelaySwitchModuleDto> RelaySwitchModules { get; set; }

    /// <summary>
    /// Таблица модулей источников напряжения и тока.
    /// </summary>
    public DbSet<PowerSourceModuleDto> PowerSourceModules { get; set; }

    /// <summary>
    /// Таблица устройств коммутации.
    /// </summary>
    public DbSet<SwitchingDeviceDto> SwitchingDevices { get; set; }

    /// <summary>
    /// Таблица быстрых измерителей.
    /// </summary>
    public DbSet<FastMeterDto> FastMeters { get; set; }

    /// <summary>
    /// Таблица пробойных установок.
    /// </summary>
    public DbSet<BreakdownTesterDto> BreakdownTesters { get; set; }

    /// <summary>
    /// Таблица стоек.
    /// </summary>
    public DbSet<RackDto> Rack { get; set; }

    ///// <summary>
    ///// Таблица бесперебойников.
    ///// </summary>
    public DbSet<UninterruptiblePowerSupplyDto> UninterruptiblePowerSupplies { get; set; }
  }
}
