using DataBaseConfiguration.Models.Device;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataBaseConfiguration.Configurations.Device
{
  public class FastMeterConfiguration : IEntityTypeConfiguration<FastMeterEntity>
  {
    /// <summary>
    /// Настройки таблицы настроек быcтрого измерителя в базе данных.
    /// </summary>
    /// <param name="builder">Объект для настройки сущности настроек быстрого измерителя в базе данных.</param>
    public void Configure(EntityTypeBuilder<FastMeterEntity> builder)
    {
      builder.HasKey(x => x.Id);
    }
  }
}
