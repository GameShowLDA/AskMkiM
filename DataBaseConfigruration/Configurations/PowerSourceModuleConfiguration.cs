using DataBaseConfiguration.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataBaseConfiguration.Configurations
{
  public class PowerSourceModuleConfiguration : IEntityTypeConfiguration<PowerSourceModuleEntity>
  {
    /// <summary>
    /// Настройки таблицы настроек МИНТа в базе данных.
    /// </summary>
    /// <param name="builder">Объект для настройки сущности настроек МИНТа в базе данных.</param>
    public void Configure(EntityTypeBuilder<PowerSourceModuleEntity> builder)
    {
      builder.HasKey(x => x.Id);
    }
  }
}
