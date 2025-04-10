using DataBaseConfiguration.Models.Device;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataBaseConfiguration.Configurations.Device
{
  public class RackConfiguration : IEntityTypeConfiguration<RackEntity>
  {
    /// <summary>
    /// Настройки таблицы настроек точного измерителя в базе данных.
    /// </summary>
    /// <param name="builder">Объект для настройки сущности настроек точного измерителя в базе данных.</param>
    public void Configure(EntityTypeBuilder<RackEntity> builder)
    {
      builder.HasKey(x => x.Id);
    }
  }
}
