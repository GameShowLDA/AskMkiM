using Ask.Core.Shared.DTO.Devices.ChassisManager;
using Ask.Core.Shared.Entity.Devices;
using Microsoft.EntityFrameworkCore;

namespace DataBaseConfiguration.Context
{
  public partial class AppDbContext
  {
    /// <summary>
    /// Таблица устройств коммутации.
    /// </summary>
    public DbSet<SwitchingDeviceEntity> SwitchingDevices { get; set; }

    /// <summary>
    /// Таблица бесперебойников.
    /// </summary>
    public DbSet<UninterruptiblePowerSupplyEntity> UninterruptiblePowerSupplies { get; set; }
  }
}
