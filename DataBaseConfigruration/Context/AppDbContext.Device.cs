using Ask.Core.Shared.DTO.Devices.ChassisManager;
using Ask.Core.Shared.Entity.Devices;
using Microsoft.EntityFrameworkCore;

namespace DataBaseConfiguration.Context
{
  public partial class AppDbContext
  {
    /// <summary>
    /// Таблица бесперебойников.
    /// </summary>
    public DbSet<UninterruptiblePowerSupplyEntity> UninterruptiblePowerSupplies { get; set; }
  }
}
